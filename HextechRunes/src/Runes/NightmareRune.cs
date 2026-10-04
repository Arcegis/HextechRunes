namespace HextechRunes;

// 梦魇(仅鸡煲) —— 黑暗充能球(DarkOrb)每次实际触发 Passive 后,对生命值最低的存活敌人造成等同于该球当前计数(EvokeVal)的伤害。
// 必须挂 DarkOrb.Passive 本体,不能只挂 BeforeTurnEndOrbTrigger:
// 漆黑等回合内效果会通过 OrbCmd.Passive 直接触发 Passive,不会经过回合结束入口。
public sealed class NightmareRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player) => IsDefectPlayer(player);

	[HarmonyPatch(typeof(DarkOrb), nameof(DarkOrb.Passive), typeof(PlayerChoiceContext), typeof(Creature))]
	[HextechPatch("rune.nightmare", "梦魇", Rune = typeof(NightmareRune))]
	private static class PassivePatch
	{
		[HarmonyPostfix]
		private static void Postfix(DarkOrb __instance, PlayerChoiceContext choiceContext, ref Task __result)
		{
			Player? player = __instance.Owner;
			if (player?.GetRelic<NightmareRune>() != null)
			{
				__result = PassiveThenNightmare(__result, __instance, choiceContext, player);
			}
		}

		// 等原版 Passive 完成后再结算；Passive 抛异常时不追加伤害。
		private static async Task PassiveThenNightmare(Task passiveTask, DarkOrb orb, PlayerChoiceContext choiceContext, Player player)
		{
			await passiveTask;
			if (player.Creature.IsDead || player.Creature.CombatState is not HextechCombatState combatState)
			{
				return;
			}

			IReadOnlyList<Creature> enemies = HextechCombatCreatureHelper.GetAliveEnemies(combatState);
			if (enemies.Count == 0)
			{
				return;
			}

			Creature weakest = enemies.MinBy(static creature => creature.CurrentHp)!;
			await CreatureCmd.Damage(choiceContext, weakest, orb.EvokeVal, ValueProp.Unpowered, player.Creature);
		}
	}
}

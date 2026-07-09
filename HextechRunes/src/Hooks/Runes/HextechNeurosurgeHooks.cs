using HarmonyLib;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

// 升级：精神过载 —— 把 NeurosurgePower 每回合开始施加的灾厄(DoomPower)从「自身」重定向到「全体存活敌人」。
internal static class HextechNeurosurgeHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NeurosurgePower), nameof(NeurosurgePower.AfterSideTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)),
			prefix: new HarmonyMethod(typeof(HextechNeurosurgeHooks), nameof(AfterSideTurnStartPrefix)));

		// 装了符文后精神过载对玩家是纯增益,但原版 Type=Debuff:
		// ①持有者实例显示/判定改为 Buff;②人工制品按 canonical 实例判 Debuff 会吞掉施加,按目标放行。
		harmony.Patch(
			RequireMethod(typeof(NeurosurgePower), "get_Type", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechNeurosurgeHooks), nameof(TypePostfix)));
		harmony.Patch(
			RequireMethod(typeof(ArtifactPower), nameof(ArtifactPower.TryModifyPowerAmountReceived), BindingFlags.Instance | BindingFlags.Public, typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(decimal).MakeByRefType()),
			prefix: new HarmonyMethod(typeof(HextechNeurosurgeHooks), nameof(ArtifactTryModifyPrefix)));
		HextechLog.Info($"[{ModInfo.Id}][Neurosurge] AfterSideTurnStart redirect + buff-type hooks installed.");
	}

	private static bool OwnsUpgradeRune(Creature? creature)
	{
		return creature?.Player?.GetRelic<NeurosurgeUpgradeRune>() != null;
	}

	private static void TypePostfix(NeurosurgePower __instance, ref PowerType __result)
	{
		if (__result == PowerType.Debuff && OwnsUpgradeRune(__instance.Owner))
		{
			__result = PowerType.Buff;
		}
	}

	private static bool ArtifactTryModifyPrefix(
		PowerModel canonicalPower,
		Creature target,
		decimal amount,
		ref decimal modifiedAmount,
		ref bool __result)
	{
		if (canonicalPower is NeurosurgePower && OwnsUpgradeRune(target))
		{
			modifiedAmount = amount;
			__result = false;
			return false;
		}

		return true;
	}

	private static bool AfterSideTurnStartPrefix(NeurosurgePower __instance, IReadOnlyList<Creature> participants, ref Task __result)
	{
		Creature? owner = __instance.Owner;
		if (owner?.Player?.GetRelic<NeurosurgeUpgradeRune>() != null && participants.Contains(owner))
		{
			__result = RedirectDoomToEnemies(__instance, owner);
			return false;
		}

		return true;
	}

	private static async Task RedirectDoomToEnemies(NeurosurgePower power, Creature owner)
	{
		if (owner.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		ThrowingPlayerChoiceContext context = new();
		foreach (Creature enemy in HextechCombatCreatureHelper.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<DoomPower>(context, enemy, power.Amount, owner, null);
		}
	}
}

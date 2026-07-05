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
		HextechLog.Info($"[{ModInfo.Id}][Neurosurge] AfterSideTurnStart redirect hook installed.");
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

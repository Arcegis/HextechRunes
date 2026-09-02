using HarmonyLib;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

// 升级：精神过载 —— 把 NeurosurgePower 每回合开始施加的灾厄(DoomPower)从「自身」重定向到「全体存活敌人」。
internal static class HextechNeurosurgeHooks
{

	private static bool OwnsUpgradeRune(Creature? creature)
	{
		return creature?.Player?.GetRelic<NeurosurgeUpgradeRune>() != null;
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

	[HarmonyPatch(typeof(NeurosurgePower), nameof(NeurosurgePower.AfterSideTurnStart), typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState))]
	[HextechPatch("rune.neurosurge.turn-start", "升级精神过载")]
	private static class AfterSideTurnStartPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(NeurosurgePower __instance, IReadOnlyList<Creature> participants, ref Task __result)
		{
			Creature? owner = __instance.Owner;
			if (owner?.Player?.GetRelic<NeurosurgeUpgradeRune>() != null && participants.Contains(owner))
			{
				__result = RedirectDoomToEnemies(__instance, owner);
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(NeurosurgePower), nameof(NeurosurgePower.Type), MethodType.Getter)]
	[HextechPatch("rune.neurosurge.type", "升级精神过载")]
	private static class TypePatch
	{
		[HarmonyPostfix]
		private static void Postfix(NeurosurgePower __instance, ref PowerType __result)
		{
			if (__result == PowerType.Debuff && OwnsUpgradeRune(__instance.Owner))
			{
				__result = PowerType.Buff;
			}
		}
	}

	[HarmonyPatch(typeof(ArtifactPower), nameof(ArtifactPower.TryModifyPowerAmountReceived), new[] { typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(decimal) }, new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
	[HextechPatch("rune.neurosurge.artifact", "升级精神过载")]
	private static class ArtifactPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(
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
	}
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechRuneMechanicHooks
{


	[HarmonyPatch(typeof(PactsEnd), "CanDealDamage", MethodType.Getter)]
	[HextechPatch("rune.pacts-end", "升级契约终结", Rune = typeof(PactsEndUpgradeRune))]
	private static class PactsEndPatch
	{
		[HarmonyPostfix]
		private static void Postfix(PactsEnd __instance, ref bool __result)
		{
			if (!__result && __instance.Owner.GetRelic<PactsEndUpgradeRune>() != null)
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(CorrosiveWavePower), nameof(CorrosiveWavePower.AfterSideTurnEnd), typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IEnumerable<Creature>))]
	[HextechPatch("rune.corrosive-wave", "升级腐蚀波", Rune = typeof(CorrosiveWaveUpgradeRune))]
	private static class CorrosiveWavePatch
	{
		[HarmonyPrefix]
		private static bool Prefix(CorrosiveWavePower __instance, ref Task __result)
		{
			if (__instance.Owner.Player?.GetRelic<CorrosiveWaveUpgradeRune>() == null)
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn), new Type[0])]
	[HextechPatch("rune.terminal-illness", "绝症", Rune = typeof(TerminalIllnessRune))]
	private static class TerminalIllnessPatch
	{
		[HarmonyPostfix]
		private static void Postfix(PoisonPower __instance, ref int __result)
		{
			HextechCombatState? combatState = __instance.Owner.CombatState;
			if (combatState == null
				|| __instance.Owner.Side != CombatSide.Enemy
				|| !combatState.Players.Any(static player =>
					player.Creature.IsAlive && player.GetRelic<TerminalIllnessRune>() != null))
			{
				return;
			}

			int triggerCount = Math.Min(
				__instance.Amount,
				1 + combatState
					.GetOpponentsOf(__instance.Owner)
					.Where(static creature => creature.IsAlive)
					.Sum(static creature => creature.GetPowerAmount<AccelerantPower>()));
			decimal totalDamage = 0m;
			for (int i = 0; i < triggerCount; i++)
			{
	#if STS2_108_OR_NEWER
				decimal damage = Hook.ModifyDamage(
					combatState.RunState,
					combatState,
					__instance.Owner,
					null,
					__instance.Amount,
					ValueProp.Unblockable | ValueProp.Unpowered,
					null,
					null,
					ModifyDamageHookType.All,
					CardPreviewMode.None,
					out _);
	#else
				decimal damage = Hook.ModifyDamage(
					combatState.RunState,
					combatState,
					__instance.Owner,
					null,
					__instance.Amount,
					ValueProp.Unblockable | ValueProp.Unpowered,
					null,
					ModifyDamageHookType.All,
					CardPreviewMode.None,
					out _);
	#endif
				totalDamage += damage;
			}

			__result = (int)totalDamage;
		}
	}

	[HarmonyPatch(typeof(ForgeCmd), nameof(ForgeCmd.Forge), typeof(decimal), typeof(Player), typeof(AbstractModel))]
	[HextechPatch("rune.big-hammer", "大锤", Rune = typeof(BigHammerRune))]
	private static class BigHammerPatch
	{
		[HarmonyPrefix]
		private static void Prefix(ref decimal amount, Player player, AbstractModel? source)
		{
			BigHammerRune? rune = player.GetRelic<BigHammerRune>();
			if (rune == null)
			{
				return;
			}

			bool sourceAlreadyIncludesBonus = source is HammerTimePower hammerTime
				&& hammerTime.Owner.Player?.GetRelic<BigHammerRune>() != null;
			decimal modifiedAmount = rune.ApplyForgeBonus(amount, sourceAlreadyIncludesBonus);
			if (modifiedAmount == amount)
			{
				return;
			}

			amount = modifiedAmount;
			rune.Flash();
		}
	}

	[HarmonyPatch(typeof(OblivionPower), nameof(OblivionPower.AfterSideTurnEnd), typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IEnumerable<Creature>))]
	[HextechPatch("rune.oblivion", "升级遗忘", Rune = typeof(OblivionUpgradeRune))]
	private static class OblivionPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(OblivionPower __instance, ref Task __result)
		{
			if (__instance.Applier?.Player?.GetRelic<OblivionUpgradeRune>() == null)
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}

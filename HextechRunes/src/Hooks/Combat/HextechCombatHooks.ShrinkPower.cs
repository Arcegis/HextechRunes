using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static void InstallShrinkPowerCompatibilityHooks(Harmony harmony)
	{
#if STS2_104_OR_NEWER
		harmony.Patch(
			RequireMethod(
				typeof(MegaCrit.Sts2.Core.Commands.PowerCmd),
				nameof(MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount),
				BindingFlags.Public | BindingFlags.Static,
				typeof(PlayerChoiceContext),
				typeof(PowerModel),
				typeof(decimal),
				typeof(Creature),
				typeof(CardModel),
				typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(ShrinkPowerModifyAmountPrefix)));
#else
		harmony.Patch(
			RequireMethod(
				typeof(MegaCrit.Sts2.Core.Commands.PowerCmd),
				nameof(MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount),
				BindingFlags.Public | BindingFlags.Static,
				typeof(PowerModel),
				typeof(decimal),
				typeof(Creature),
				typeof(CardModel),
				typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(ShrinkPowerModifyAmountPrefix)));
#endif
	}

	private static bool ShrinkPowerModifyAmountPrefix(
#if STS2_104_OR_NEWER
		PlayerChoiceContext choiceContext,
#endif
		PowerModel power,
		decimal offset,
		Creature? applier,
		CardModel? cardSource,
		bool silent,
		ref Task<int> __result)
	{
		if (!ShouldReplaceTemporaryShrinkWithPermanent(power, offset, applier))
		{
			return true;
		}

		__result = ReplaceTemporaryShrinkWithPermanent(
#if STS2_104_OR_NEWER
			choiceContext,
#endif
			power,
			offset,
			applier,
			cardSource,
			silent);
		return false;
	}

	private static bool ShouldReplaceTemporaryShrinkWithPermanent(PowerModel power, decimal offset, Creature? applier)
	{
		return power is ShrinkPower
			&& power.Amount > 0
			&& offset < 0m
			&& power.Owner.Side == CombatSide.Player
			&& applier?.Side == CombatSide.Enemy
			&& power.Owner.GetPowerAmount<ArtifactPower>() <= 0;
	}

	private static async Task<int> ReplaceTemporaryShrinkWithPermanent(
#if STS2_104_OR_NEWER
		PlayerChoiceContext choiceContext,
#endif
		PowerModel temporaryShrink,
		decimal permanentOffset,
		Creature? applier,
		CardModel? cardSource,
		bool silent)
	{
		Creature owner = temporaryShrink.Owner;
		await MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(temporaryShrink);
#if STS2_104_OR_NEWER
		ShrinkPower? permanentShrink = await MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<ShrinkPower>(
			choiceContext,
			owner,
			permanentOffset,
			applier,
			cardSource,
			silent);
#else
		ShrinkPower? permanentShrink = await MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<ShrinkPower>(
			owner,
			permanentOffset,
			applier,
			cardSource,
			silent);
#endif
		return permanentShrink?.Amount ?? 0;
	}
}

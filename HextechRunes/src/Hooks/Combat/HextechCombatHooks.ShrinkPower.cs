using HarmonyLib;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static void InstallShrinkPowerCompatibilityHooks(Harmony harmony)
	{
		harmony.Patch(
			HextechPowerCmdCompat.RequireModifyAmountMethod(),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(ShrinkPowerModifyAmountPrefix)));
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

		object? effectiveChoiceContext = null;
#if STS2_104_OR_NEWER
		effectiveChoiceContext = choiceContext;
#endif

		__result = ReplaceTemporaryShrinkWithPermanent(
			effectiveChoiceContext,
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
		object? choiceContext,
		PowerModel temporaryShrink,
		decimal permanentOffset,
		Creature? applier,
		CardModel? cardSource,
		bool silent)
	{
		Creature owner = temporaryShrink.Owner;
		await HextechPowerCmdCompat.Remove(temporaryShrink);
		ShrinkPower? permanentShrink = await HextechPowerCmdCompat.Apply<ShrinkPower>(
			choiceContext,
			owner,
			permanentOffset,
			applier,
			cardSource,
			silent);
		return permanentShrink?.Amount ?? 0;
	}
}

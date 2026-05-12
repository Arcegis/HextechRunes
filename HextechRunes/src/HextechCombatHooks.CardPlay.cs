using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static void CardCanPlayPostfix(CardModel __instance, ref bool __result)
	{
		if (__result && IsBlockedByBackToBasics(__instance))
		{
			__result = false;
		}
	}

	private static void CardCanPlayWithReasonPostfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel preventer)
	{
		if (!__result)
		{
			return;
		}

		if (!IsBlockedByBackToBasics(__instance, out AbstractModel? backToBasicsPreventer))
		{
			return;
		}

		reason = default;
		preventer = backToBasicsPreventer!;
		__result = false;
	}

	private static bool IsBlockedByBackToBasics(CardModel card)
	{
		return IsBlockedByBackToBasics(card, out _);
	}

	private static bool IsBlockedByBackToBasics(CardModel card, out AbstractModel? preventer)
	{
		preventer = null;
		if (card.Owner == null)
		{
			return false;
		}

		if (card.EnergyCost.CostsX || GetEnergyCostForCurrentCardPlay(card) < 3m)
		{
			return false;
		}

		BackToBasicsRune? rune = card.Owner.GetRelic<BackToBasicsRune>();
		if (rune != null)
		{
			preventer = rune;
			return true;
		}

		if (card.Owner.Creature.CombatState?.RunState is RunState runState
			&& card.Owner.Creature.Side == CombatSide.Player
			&& GetMayhemModifier(runState) is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.BackToBasics))
		{
			preventer = modifier;
			return true;
		}

		return false;
	}
}

using MegaCrit.Sts2.Core.Combat.History;

namespace HextechRunes;

internal static class HextechCombatHistoryHelper
{
	public static int CountOwnedAttackCardsPlayed(Player? owner, bool firstInSeriesOnly = true, bool includeAutoPlay = false)
	{
		return CountOwnedCardsPlayed(
			owner,
			card => HextechCardEffectTypes.IsAttackForEffects(card, owner),
			firstInSeriesOnly,
			includeAutoPlay);
	}

	public static int CountOwnedCardsPlayed(Player? owner, Func<CardModel, bool> matches, bool firstInSeriesOnly = true, bool includeAutoPlay = false)
	{
		if (owner == null)
		{
			return 0;
		}

		ulong ownerId = owner.NetId;
		return CombatManager.Instance.History.Entries
			.OfType<CardPlayFinishedEntry>()
			.Count(entry =>
				(!firstInSeriesOnly || entry.CardPlay.IsFirstInSeries)
				&& (includeAutoPlay || !entry.CardPlay.IsAutoPlay)
				&& entry.CardPlay.Card.Owner?.NetId == ownerId
				&& matches(entry.CardPlay.Card));
	}

	public static int CountOwnedAttackCardsPlayedThisTurn(Player? owner, HextechCombatState? combatState, bool firstInSeriesOnly = true, bool includeAutoPlay = false)
	{
		if (owner == null || combatState == null)
		{
			return 0;
		}

		ulong ownerId = owner.NetId;
		return CombatManager.Instance.History.CardPlaysFinished
			.Count(entry =>
				HappenedThisTurn(entry, combatState)
				&& (!firstInSeriesOnly || entry.CardPlay.IsFirstInSeries)
				&& (includeAutoPlay || !entry.CardPlay.IsAutoPlay)
				&& entry.CardPlay.Card.Owner?.NetId == ownerId
				&& HextechCardEffectTypes.IsAttackForEffects(entry.CardPlay.Card, owner));
	}

	private static bool HappenedThisTurn(CombatHistoryEntry entry, HextechCombatState? combatState)
	{
		return entry.HappenedThisTurn(combatState);
	}

	public static int CountOwnedCardsDrawn(Player? owner)
	{
		if (owner == null)
		{
			return 0;
		}

		ulong ownerId = owner.NetId;
		return CombatManager.Instance.History.Entries
			.OfType<CardDrawnEntry>()
			.Count(entry => entry.Card.Owner?.NetId == ownerId);
	}

	public static bool IsDamageFromOwner(Player? owner, Creature? dealer, CardModel? cardSource)
	{
		if (owner == null)
		{
			return false;
		}

		if (IsOwnerOrPet(owner, dealer))
		{
			return true;
		}

		if (dealer?.Side == CombatSide.Player)
		{
			return false;
		}

		Player? cardOwner = cardSource?.Owner;
		if (cardOwner == null)
		{
			return false;
		}

		return HextechPlayerContextHelper.IsNetworkMultiplayerRun()
			? cardOwner.NetId == owner.NetId
			: cardOwner == owner;
	}

	public static bool IsOwnerOrPet(Player? owner, Creature? dealer)
	{
		return owner != null && (dealer == owner.Creature || dealer?.PetOwner == owner);
	}
}

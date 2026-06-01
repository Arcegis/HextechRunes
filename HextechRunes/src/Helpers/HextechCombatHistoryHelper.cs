using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

internal static class HextechCombatHistoryHelper
{
	public static int CountOwnedAttackCardsPlayed(Player? owner, bool firstInSeriesOnly = true, bool includeAutoPlay = false)
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
				&& IllusoryWeaponRune.IsAttackForEffects(entry.CardPlay.Card, owner));
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

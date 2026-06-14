using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public abstract partial class HextechRelicBase
{
	protected static bool DeckContains<TCard>(Player player)
		where TCard : CardModel
	{
		ModelId cardId = ModelDb.GetId<TCard>();
		return player.Deck.Cards.Any(card => (card.CanonicalInstance?.Id ?? card.Id) == cardId);
	}

	protected static int FloorToInt(decimal value)
	{
		return (int)decimal.Floor(value);
	}

	protected bool IsOwnedCard(CardModel? card)
	{
		return card?.Owner == Owner;
	}

	protected bool IsOwnedAttack(CardModel? card)
	{
		return Owner != null && card?.Owner == Owner && IllusoryWeaponRune.IsAttackForEffects(card, Owner);
	}

	protected bool IsOwnedSkill(CardModel? card)
	{
		return card != null && card.Owner == Owner && IllusoryWeaponRune.IsSkillForEffects(card);
	}

	protected bool IsAttackDamageForRuneEffects(ValueProp props, CardModel? cardSource)
	{
		if (HextechSts2Compat.IsPoweredAttack(props))
		{
			return true;
		}

		return Owner != null && IllusoryWeaponRune.IsOriginalOwnedSkill(cardSource, Owner);
	}

	protected int CountOwnedAttackCardsPlayedFromHistory(bool firstInSeriesOnly = true, bool includeAutoPlay = false)
	{
		return HextechCombatHistoryHelper.CountOwnedAttackCardsPlayed(Owner, firstInSeriesOnly, includeAutoPlay);
	}

	protected int CountOwnedCardsDrawnFromHistory()
	{
		return HextechCombatHistoryHelper.CountOwnedCardsDrawn(Owner);
	}

	protected bool IsOwnedNonXCardWithCostAtLeast(CardModel? card, decimal minimumCost)
	{
		return card != null
			&& card.Owner == Owner
			&& !card.EnergyCost.CostsX
			&& HextechCombatHooks.GetEnergyCostForCurrentCardPlay(card) >= minimumCost;
	}

	protected bool IsOwnerOrPet(Creature? dealer)
	{
		return HextechCombatHistoryHelper.IsOwnerOrPet(Owner, dealer);
	}

	protected bool IsDamageFromOwner(Creature? dealer, CardModel? cardSource)
	{
		return HextechCombatHistoryHelper.IsDamageFromOwner(Owner, dealer, cardSource);
	}

	protected bool TryGetOwnedEnemyDebuffTarget(PowerModel power, decimal amount, Creature? applier, out Creature? target)
	{
		target = power.Owner;
		return amount > 0m
			&& target?.Side == CombatSide.Enemy
			&& applier == Owner?.Creature
			&& power.GetTypeForAmount(amount) == PowerType.Debuff
			&& power is not ITemporaryPower;
	}
}

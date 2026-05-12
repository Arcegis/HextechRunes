using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ForgottenSoulRune : HextechRelicBase
{
	private bool _preventedExhaustLastPlay;

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		_preventedExhaustLastPlay = false;
		if (card.Owner == Owner
			&& pileType == PileType.Exhaust
			&& card.Keywords.Contains(CardKeyword.Exhaust))
		{
			_preventedExhaustLastPlay = true;
			return (PileType.Discard, position);
		}

		return (pileType, position);
	}

	public override Task AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
		if (_preventedExhaustLastPlay && card.Owner == Owner)
		{
			Flash();
		}

		_preventedExhaustLastPlay = false;
		return Task.CompletedTask;
	}
}

public sealed class GoodLuckRune : HextechRelicBase
{
	public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player != Owner
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count == 0)
		{
			return false;
		}

		HashSet<ModelId> existingIds = cardRewardOptions
			.Select(static result => result.Card.CanonicalInstance.Id)
			.ToHashSet();
		List<CardModel> rarePool = creationOptions
			.GetPossibleCards(player)
			.Where(card => card.Rarity == CardRarity.Rare && !existingIds.Contains(card.Id))
			.ToList();
		if (rarePool.Count == 0)
		{
			return false;
		}

		CardCreationOptions rareOptions = new CardCreationOptions(
				rarePool,
				creationOptions.Source,
				CardRarityOddsType.Uniform)
			.WithFlags(CardCreationFlags.NoModifyHooks);
		CardCreationResult? rareResult = CardFactory.CreateForReward(player, 1, rareOptions).FirstOrDefault();
		if (rareResult == null)
		{
			return false;
		}

		cardRewardOptions.Add(rareResult);
		Flash();
		return true;
	}
}

public sealed class ManipulateRealityRune : HextechRelicBase
{
#if STS2_104_OR_NEWER
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
		if (card.Owner != Owner || !card.IsUpgradable)
		{
			return Task.CompletedTask;
		}

		CardCmd.Upgrade(card, CardPreviewStyle.None);
		Flash();
		return Task.CompletedTask;
	}
}

public sealed class CarefulSelectionRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(4)
	];

	public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (player != Owner || room is not CombatRoom)
		{
			return false;
		}

		bool modified = false;
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] is not CardReward cardReward)
			{
				continue;
			}

			rewards[i] = new CardReward(
				CardCreationOptions.ForRoom(player, room.RoomType),
				DynamicVars.Cards.IntValue,
				player)
			{
				CanReroll = cardReward.CanReroll
			};
			modified = true;
		}

		if (modified)
		{
			Flash();
		}

		return modified;
	}
}

public sealed class CatalystRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<CatalystCard>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<CatalystCard>(DynamicVars.Cards.IntValue);
	}
}

public sealed class UpgradeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		List<CardModel> cards = Owner.Deck.Cards
			.Where(static card => card.IsUpgradable)
			.ToList();
		if (cards.Count > 0)
		{
			Flash();
			foreach (CardModel card in cards)
			{
				CardCmd.Upgrade(card);
			}
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player != Owner || cardRewardOptions.Count == 0)
		{
			return false;
		}

		bool modified = false;
		foreach (CardCreationResult result in cardRewardOptions)
		{
			CardModel card = result.Card;
			if (!card.IsUpgradable)
			{
				continue;
			}

			CardCmd.Upgrade(card, CardPreviewStyle.None);
			result.ModifyCard(card, this);
			modified = true;
		}

		return modified;
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (card.Owner != Owner || !card.IsUpgradable)
		{
			return false;
		}

		CardCmd.Upgrade(card, CardPreviewStyle.None);
		newCard = card;
		Flash();
		return true;
	}
}

public sealed class PowerShieldRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (_triggeredThisTurn || Owner == null || creature != Owner.Creature || amount <= 0m)
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		Flash();
		int strength = Math.Max(1, Owner.RunState.CurrentActIndex + 1);
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, cardSource);
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class CorrosionRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m),
		new PowerVar<DexterityPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (_triggeredThisTurn
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| !target.IsAlive
			|| result.TotalDamage <= 0m
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		Flash([target]);
		await PowerCmd.Apply<StrengthPower>(target, -DynamicVars.Strength.BaseValue, Owner.Creature, cardSource);
		await PowerCmd.Apply<DexterityPower>(target, -DynamicVars.Dexterity.BaseValue, Owner.Creature, cardSource);
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

public sealed class RadianceRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpDamagePercent", 0.05m),
		new HealVar(1m)
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (_triggeredThisTurn
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.TotalDamage <= 0m
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		_triggeredThisTurn = true;
		UpdateTurnScopedStateIdentity();
		if (target.IsAlive)
		{
			int damage = Math.Max(1, FloorToInt(target.MaxHp * DynamicVars["MaxHpDamagePercent"].BaseValue));
			Flash([target]);
			await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered, Owner.Creature, cardSource);
		}
		else
		{
			Flash();
		}

		if (Owner.Creature.IsAlive)
		{
			await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
		}
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}

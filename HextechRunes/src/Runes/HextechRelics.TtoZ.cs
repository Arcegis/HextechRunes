using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

public sealed class TwiceThriceRune : HextechRelicBase
{
	private const int AttacksPerReplay = 3;

	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetAttacksPlayedThisCombat();
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value) % AttacksPerReplay;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			return GetAttacksPlayedThisCombat();
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (!IsOwnedAttack(card))
		{
			return playCount;
		}

		int nextAttacksPlayed = GetAttacksPlayedBeforeCurrentAttack() + 1;
		_attacksPlayedThisCombat = nextAttacksPlayed % AttacksPerReplay;
		if (nextAttacksPlayed % AttacksPerReplay == 0)
		{
			InvokeDisplayAmountChanged();
			return playCount + 1;
		}

		InvokeDisplayAmountChanged();
		return playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (IsOwnedAttack(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private void ResetAttacksPlayedThisCombat()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}

	private int GetAttacksPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory() % AttacksPerReplay
			: _attacksPlayedThisCombat;
	}

	private int GetAttacksPlayedBeforeCurrentAttack()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory()
			: _attacksPlayedThisCombat;
	}
}

public sealed class UltimateRefreshRune : HextechRelicBase
{
	private bool _triggeredLastPlay;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this was a transient flash flag and must not enter multiplayer checksums.
			_triggeredLastPlay = false;
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetTurnState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState();
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

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		_triggeredLastPlay = false;
		if (card.Owner != Owner)
		{
			return playCount;
		}

		if (!IsOwnedNonXCardWithCostAtLeast(card, 2m))
		{
			return playCount;
		}

		_triggeredLastPlay = true;
		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (_triggeredLastPlay && IsOwnedNonXCardWithCostAtLeast(card, 2m))
		{
			Flash();
			_triggeredLastPlay = false;
		}

		return Task.CompletedTask;
	}

	private void ResetTurnState(HextechCombatState? combatState = null)
	{
		_triggeredLastPlay = false;
	}
}

public sealed class UltimateUnstoppableRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[
			new DynamicVar("MinCost", 2m),
			new PowerVar<ArtifactPower>(2m)
		];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || !IsOwnedNonXCardWithCostAtLeast(cardPlay.Card, DynamicVars["MinCost"].BaseValue))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, cardPlay.Card);
	}
}

public sealed class UnmovableMountainRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BarricadePower>(1m),
		new PowerVar<AfterimagePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BarricadePower>(),
		HoverTipFactory.FromPower<AfterimagePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<BarricadePower>(Owner.Creature, DynamicVars["BarricadePower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<AfterimagePower>(Owner.Creature, DynamicVars["AfterimagePower"].BaseValue, Owner.Creature, null);
	}
}

public sealed class UnyieldingArmorRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>()
	];

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (Owner == null || target != Owner.Creature || canonicalPower is not PlatingPower || amount >= 0m)
		{
			return false;
		}

		modifiedAmount = 0m;
		Flash();
		return true;
	}
}

public sealed class WarmogsSpiritRune : HextechRelicBase
{
	private const int CardsNeeded = 8;

	private int _cardsDrawnThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCardsDrawnThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetCardsDrawnThisCombat();
		set
		{
			_cardsDrawnThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			int remainder = GetCardsDrawnThisCombat() % CardsNeeded;
			return remainder == 0 ? CardsNeeded : CardsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CardsNeeded", CardsNeeded),
		new PowerVar<PlatingPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>()
	];

	public override Task BeforeCombatStart()
	{
		_cardsDrawnThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_cardsDrawnThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner != Owner)
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveDrawProgressFromHistory();
			return;
		}

		_cardsDrawnThisCombat++;
		InvokeDisplayAmountChanged();
		if (Owner == null || Owner.Creature.IsDead || _cardsDrawnThisCombat % CardsNeeded != 0)
		{
			return;
		}

		await ApplyDrawThresholdReward();
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && cardPlay.Card.Owner == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (ShouldUseNetworkCombatHistory() && player == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

#if !STS2_104_OR_NEWER
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (ShouldUseNetworkCombatHistory() && player == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}
#endif

	private async Task ResolveDrawProgressFromHistory()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int cardsDrawn = CountOwnedCardsDrawnFromHistory();
		int previousCardsDrawn = _cardsDrawnThisCombat;
		if (cardsDrawn <= previousCardsDrawn)
		{
			return;
		}

		_cardsDrawnThisCombat = cardsDrawn;
		InvokeDisplayAmountChanged();
		int rewards = cardsDrawn / CardsNeeded - previousCardsDrawn / CardsNeeded;
		for (int i = 0; i < rewards; i++)
		{
			await ApplyDrawThresholdReward();
		}
	}

	private async Task ApplyDrawThresholdReward()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
	}

	private int GetCardsDrawnThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedCardsDrawnFromHistory()
			: _cardsDrawnThisCombat;
	}
}

public sealed class WatchOutGrapefruitRune : HextechRelicBase
{
	private static readonly Type[] FoodRelicTypes =
	[
		typeof(Strawberry),
		typeof(Pear),
		typeof(Mango),
		typeof(DragonFruit),
		typeof(LoomingFruit),
		typeof(LeesWaffle),
		typeof(YummyCookie),
		typeof(MeatOnTheBone),
		typeof(PaelsFlesh),
		typeof(IceCream),
		typeof(Bread),
		typeof(NutritiousOyster),
		typeof(VeryHotCocoa),
		typeof(FragrantMushroom),
		typeof(BigMushroom)
	];

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

			Type[] candidates = Owner.GetRelic<IceCream>() == null
				? FoodRelicTypes
				: FoodRelicTypes.Where(static type => type != typeof(IceCream)).ToArray();
			Type relicType = HextechStableRandom.Pick(
				candidates,
				(RunState)Owner.RunState,
				HextechStableRandom.TypeModelKey,
				"treat-yourself-food-relic",
				HextechStableRandom.PlayerKey(Owner),
				Owner.Relics.Count.ToString());
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(relicType)).ToMutable();
		Flash(Array.Empty<Creature>());
		room.AddExtraReward(Owner, new RelicReward(relic, Owner));
		return Task.CompletedTask;
	}
}

public sealed class WizardlyThinkingRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null || !IsDefectOwner)
		{
			return;
		}

		int focus = Owner.RunState.CurrentActIndex + 1;
		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, focus, Owner.Creature, null);
	}
}

public sealed class WraithRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
		new DynamicVar("DamagePercentPerSoul", 3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		IEnumerable<Soul> souls = Soul.Create(Owner, DynamicVars.Cards.IntValue, combatState);
		await HextechCardGeneration.AddGeneratedCardsToCombat(souls, PileType.Hand, addedByPlayer: true);
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		int soulCount = Owner?.PlayerCombatState?.ExhaustPile.Cards.Count(static card => card is Soul) ?? 0;
		return 1m + soulCount * DynamicVars["DamagePercentPerSoul"].BaseValue / 100m;
	}
}

public sealed class ZealotRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("RelicsNeeded", 5m),
		new CardsVar(1)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner || player.Creature.CombatState?.RoundNumber > 1)
		{
			return count;
		}

		return count + Math.Floor(player.Relics.Count / DynamicVars["RelicsNeeded"].BaseValue);
	}
}

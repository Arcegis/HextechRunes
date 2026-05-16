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

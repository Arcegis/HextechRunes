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

public sealed class RekindleRune : HextechRelicBase
{
	private int _exhaustedCardsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedExhaustedCardsThisCombat
	{
		get => _exhaustedCardsThisCombat;
		set
		{
			_exhaustedCardsThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars.Cards.IntValue - _exhaustedCardsThisCombat) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new EnergyVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!IsOwnedCard(card) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		_exhaustedCardsThisCombat++;
		int energyToGain = 0;
		while (_exhaustedCardsThisCombat >= DynamicVars.Cards.IntValue)
		{
			_exhaustedCardsThisCombat -= DynamicVars.Cards.IntValue;
			energyToGain++;
		}

		InvokeDisplayAmountChanged();
		if (energyToGain <= 0)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue * energyToGain, Owner);
	}

	private void ResetCounter()
	{
		_exhaustedCardsThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ShoulderVakuRune : HextechRelicBase
{
	private static readonly ModelId WhisperingEarringId = ModelDb.GetId<WhisperingEarring>();

	private int _lastControlledRound;
	private bool _controllingTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(2),
		new CardsVar(2),
		new DynamicVar("HealPercent", 5m)
	];

	public override Task BeforeCombatStart()
	{
		_lastControlledRound = 0;
		_controllingTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_lastControlledRound = 0;
		_controllingTurn = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? amount + DynamicVars.Energy.BaseValue : amount;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsOddOwnerTurn(player, out _))
		{
			return;
		}

		int heal = Math.Max(1, FloorToInt(Owner!.Creature.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		Flash();
		await CreatureCmd.Heal(Owner.Creature, heal);
	}

#if STS2_104_OR_NEWER
	public override Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
#else
	public override Task BeforePlayPhaseStartLate(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		return ControlOddTurnWithVakuu(choiceContext, player);
	}

	private async Task ControlOddTurnWithVakuu(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsOddOwnerTurn(player, out int round) || _lastControlledRound == round || _controllingTurn)
		{
			return;
		}

		if (round == 1 && OwnerHasWhisperingEarring())
		{
			return;
		}

		_lastControlledRound = round;
		_controllingTurn = true;
		try
		{
			Flash();
			int cardsPlayed = await VakuuTurnController.AutoPlayPlayableHand(Owner!);
			VakuuTurnController.PlayLineIfCardsPlayed(Owner!, cardsPlayed);
		}
		finally
		{
			_controllingTurn = false;
		}
	}

	private bool IsOddOwnerTurn(Player player, out int round)
	{
		round = 0;
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return false;
		}

		round = Owner.Creature.CombatState.RoundNumber;
		return round > 0 && round % 2 == 1;
	}

	private bool OwnerHasWhisperingEarring()
	{
		return Owner?.Relics.Any(static relic =>
			(relic.CanonicalInstance?.Id ?? relic.Id) == WhisperingEarringId) == true;
	}
}

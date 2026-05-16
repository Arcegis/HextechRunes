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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ShoulderVakuRune : HextechRelicBase
{
	private int _lastControlledRound;
	private bool _controllingTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(2),
		new CardsVar(2),
		new DynamicVar("HealPercent", 10m)
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

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
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

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (!IsOddOwnerTurn(player, out int round) || _lastControlledRound == round || _controllingTurn)
		{
			return;
		}

		_lastControlledRound = round;
		_controllingTurn = true;
		try
		{
			Flash();
			using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
			{
				await AutoPlayPlayableHand(choiceContext, round);
			}
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

	private async Task AutoPlayPlayableHand(PlayerChoiceContext choiceContext, int round)
	{
		if (Owner == null || Owner.Creature.CombatState == null)
		{
			return;
		}

		List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Owner == Owner && card.Type is CardType.Attack or CardType.Skill or CardType.Power)
			.ToList();
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel card = cards[i];
			if (card.Pile?.Type != PileType.Hand || !card.CanPlay())
			{
				continue;
			}

			Creature? target = HextechRuneTargeting.PickRandomHittableEnemy(
				Owner,
				Owner.Creature.CombatState,
				"shoulder-vaku",
				round.ToString(),
				i.ToString(),
				CombatManager.Instance.History.Entries.Count().ToString());
			await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default);
		}
	}
}

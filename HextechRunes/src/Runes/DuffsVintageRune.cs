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

public sealed class DuffsVintageRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CostReduction", 1m)
	];

	public override bool ShouldFlush(Player player)
	{
		return player != Owner;
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
			.Where(CanReduceCost)
			.ToList();
		if (cards.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			int reduction = DynamicVars["CostReduction"].IntValue;
			if (!card.EnergyCost.CostsX)
			{
				int currentCostBeforeGlobalModifiers = card.EnergyCost.GetWithModifiers(CostModifiers.Local);
				int nextCost = Math.Max(0, currentCostBeforeGlobalModifiers - reduction);
				card.EnergyCost.SetUntilPlayed(nextCost, reduceOnly: true);
			}
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (Owner == null
			|| card.Owner != Owner
			|| card.HasStarCostX
			|| originalCost <= 0m
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return false;
		}

		int reduction = Math.Max(0, combatState.RoundNumber - 1) * DynamicVars["CostReduction"].IntValue;
		if (reduction <= 0)
		{
			return false;
		}

		modifiedCost = Math.Max(0m, originalCost - reduction);
		return modifiedCost != originalCost;
	}

	private static bool CanReduceCost(CardModel card)
	{
		return !card.EnergyCost.CostsX || (!card.HasStarCostX && card.CurrentStarCost > 0);
	}
}

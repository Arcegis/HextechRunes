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

public sealed class GrowingStrongerRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (Owner == null
			|| power.Owner != Owner.Creature
			|| power.GetType() != typeof(StrengthPower)
			|| amount <= 0m
			|| Owner.PlayerCombatState == null)
		{
			return Task.CompletedTask;
		}

		int cardsToFree = FloorToInt(amount);
		if (cardsToFree <= 0)
		{
			return Task.CompletedTask;
		}

		bool freedAny = false;
		for (int i = 0; i < cardsToFree; i++)
		{
			CardModel? card = PickCardToMakeFree(i, cardsToFree);
			if (card == null)
			{
				break;
			}

			card.SetToFreeThisTurn();
			freedAny = true;
		}

		if (freedAny)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private CardModel? PickCardToMakeFree(int ordinal, int total)
	{
		if (Owner?.PlayerCombatState == null)
		{
			return null;
		}

		IReadOnlyList<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards;
		return PickCardToMakeFreeFromCandidates(
				handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: false)).ToList(),
				ordinal,
				total,
				includeGlobalModifiers: false)
			?? PickCardToMakeFreeFromCandidates(
				handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList(),
				ordinal,
				total,
				includeGlobalModifiers: true);
	}

	private CardModel? PickCardToMakeFreeFromCandidates(IReadOnlyList<CardModel> candidates, int ordinal, int total, bool includeGlobalModifiers)
	{
		if (Owner == null || candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)Owner.RunState,
			candidates.Count,
			"guinsoos-rageblade-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			ordinal.ToString(),
			total.ToString(),
			includeGlobalModifiers ? "global" : "base",
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}

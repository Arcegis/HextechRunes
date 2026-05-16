using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class OmniDragonSoulRune : HextechRelicBase
{
	private const int DragonSoulCardKinds = 6;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<OceanDragonSoulCard>(),
		HoverTipFactory.FromCard<InfernalDragonSoulCard>(),
		HoverTipFactory.FromCard<HextechDragonSoulCard>(),
		HoverTipFactory.FromCard<MountainDragonSoulCard>(),
		HoverTipFactory.FromCard<ChemtechDragonSoulCard>(),
		HoverTipFactory.FromCard<CloudDragonSoulCard>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null
			|| Owner.PlayerCombatState == null
			|| Owner.Creature.CombatState is not HextechCombatState combatState
			|| !CombatManager.Instance.IsInProgress
			|| CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}

		Flash();
		await AddRandomUpgradedDragonSoulCardsToCombatHand(DynamicVars.Cards.IntValue, combatState);
	}

	private async Task AddRandomUpgradedDragonSoulCardsToCombatHand(int count, HextechCombatState combatState)
	{
		if (Owner == null || count <= 0)
		{
			return;
		}

		IReadOnlyList<int> dragonSoulRolls = RollDistinctDragonSoulCardKinds(count, combatState);
		List<CardModel> cards = new(dragonSoulRolls.Count);
		foreach (int roll in dragonSoulRolls)
		{
			cards.Add(CreateUpgradedDragonSoulCard(combatState, roll));
		}

		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}

	private IReadOnlyList<int> RollDistinctDragonSoulCardKinds(int count, HextechCombatState? combatState)
	{
		Player owner = Owner ?? throw new InvalidOperationException("Omni Dragon Soul rolled cards without an owner.");
		return HextechStableRandom.PickDistinct(
			Enumerable.Range(0, DragonSoulCardKinds),
			count,
			(RunState)owner.RunState,
			static roll => roll.ToString(),
			"omni-dragon-soul-card",
			HextechStableRandom.PlayerKey(owner),
			combatState?.RoundNumber.ToString() ?? "-1",
			owner.Deck.Cards.Count.ToString());
	}

	private CardModel CreateUpgradedDragonSoulCard(HextechCombatState? combatState, int roll)
	{
		CardModel card = CreateDragonSoulCard(combatState, roll);
		CardCmd.Upgrade(card);
		return card;
	}

	private CardModel CreateDragonSoulCard(HextechCombatState? combatState, int roll)
	{
		Player owner = Owner ?? throw new InvalidOperationException("Omni Dragon Soul created a card without an owner.");
		if (combatState != null)
		{
			return roll switch
			{
				0 => combatState.CreateCard<OceanDragonSoulCard>(owner),
				1 => combatState.CreateCard<InfernalDragonSoulCard>(owner),
				2 => combatState.CreateCard<HextechDragonSoulCard>(owner),
				3 => combatState.CreateCard<MountainDragonSoulCard>(owner),
				4 => combatState.CreateCard<ChemtechDragonSoulCard>(owner),
				_ => combatState.CreateCard<CloudDragonSoulCard>(owner)
			};
		}

		return roll switch
		{
			0 => owner.RunState.CreateCard<OceanDragonSoulCard>(owner),
			1 => owner.RunState.CreateCard<InfernalDragonSoulCard>(owner),
			2 => owner.RunState.CreateCard<HextechDragonSoulCard>(owner),
			3 => owner.RunState.CreateCard<MountainDragonSoulCard>(owner),
			4 => owner.RunState.CreateCard<ChemtechDragonSoulCard>(owner),
			_ => owner.RunState.CreateCard<CloudDragonSoulCard>(owner)
		};
	}
}

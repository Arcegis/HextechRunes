using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public abstract class UniversalScopeRuneBase : HextechRelicBase
{
	protected abstract int ChancePercent { get; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ChancePercent", ChancePercent)
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !IsOwnedAttack(cardPlay.Card))
		{
			return;
		}

		if (!RollTrigger(cardPlay))
		{
			return;
		}

		HextechCardPlayResourceSpend resourceSpend = HextechCombatHooks.GetResourceSpendForCurrentCardPlay(cardPlay.Card);
		Flash();
		if (cardPlay.Card.Pile?.Type == PileType.Play)
		{
			await CardPileCmd.Add(cardPlay.Card, PileType.Hand, CardPilePosition.Bottom, this);
		}

		if (resourceSpend.Energy > 0m)
		{
			await PlayerCmd.GainEnergy(resourceSpend.Energy, Owner);
		}

		if (resourceSpend.Stars > 0m)
		{
			await PlayerCmd.GainStars(resourceSpend.Stars, Owner);
		}
	}

	private bool RollTrigger(CardPlay cardPlay)
	{
		if (Owner == null)
		{
			return false;
		}

		return HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["ChancePercent"].IntValue,
			"universal-scope-refund",
			GetType().Name,
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			CombatManager.Instance.History.Entries.Count().ToString(),
			HextechStableRandom.CardKey(cardPlay.Card));
	}
}

public sealed class UniversalScopeRune : UniversalScopeRuneBase
{
	protected override int ChancePercent => 15;
}

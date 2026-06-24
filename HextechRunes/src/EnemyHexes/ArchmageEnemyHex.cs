namespace HextechRunes;

internal sealed class ArchmageEnemyHex : HextechEnemyHexEffect
{
	private const int ChancePercent = 33;

	internal override MonsterHexKind Kind => MonsterHexKind.Archmage;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player? owner = cardPlay.Card.Owner;
		if (owner?.Creature.Side != CombatSide.Player
			|| owner.Creature.CombatState?.RunState != context.RunState
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card)
			|| !RollTrigger(context, owner, cardPlay.Card, out int rollOrdinal)
			|| PickCard(owner, cardPlay.Card, rollOrdinal) is not CardModel card
			|| card.EnergyCost.CostsX)
		{
			return Task.CompletedTask;
		}

		card.EnergyCost.AddThisTurnOrUntilPlayed(1, reduceOnly: false);
		return Task.CompletedTask;
	}

	private static bool RollTrigger(HextechEnemyHexContext context, Player owner, CardModel sourceCard, out int rollOrdinal)
	{
		rollOrdinal = HextechCombatProcTracker.ConsumeGlobalProcInCombat(
			context.Tracking,
			string.Join(":", nameof(ArchmageEnemyHex), HextechStableRandom.PlayerKey(owner)));
		return HextechStableRandom.PercentChance(
			(RunState)context.RunState,
			ChancePercent,
			"enemy-archmage-cost-up",
			HextechStableRandom.PlayerKey(owner),
			owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			rollOrdinal.ToString(),
			HextechStableRandom.CardKey(sourceCard));
	}

	private static CardModel? PickCard(Player owner, CardModel sourceCard, int rollOrdinal)
	{
		IReadOnlyList<CardModel> candidates = PileType.Hand.GetPile(owner).Cards
			.Where(static card => !card.EnergyCost.CostsX)
			.ToList();
		if (candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)owner.RunState,
			candidates.Count,
				"enemy-archmage-pick-card",
				HextechStableRandom.PlayerKey(owner),
				owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
				rollOrdinal.ToString(),
				HextechStableRandom.CardKey(sourceCard),
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}

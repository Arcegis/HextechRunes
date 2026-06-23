namespace HextechRunes;

internal sealed class BlankCheckEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BlankCheck;

	internal override Task AfterPlayerTurnStartLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return Task.CompletedTask;
		}

		List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
			.Where(static card => card.Pile?.Type == PileType.Hand && !card.Keywords.Contains(CardKeyword.Ethereal))
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (candidates.Count == 0)
		{
			return Task.CompletedTask;
		}

		int count = Math.Min(candidates.Count, context.TierValue(Kind, 1, 2, 3));
		List<CardModel> selected = HextechStableRandom.PickDistinct(
			candidates,
			count,
			(RunState)context.RunState,
			HextechStableRandom.CardKey,
			"enemy-blank-check-ethereal",
			HextechStableRandom.PlayerKey(player),
			combatState.RoundNumber.ToString());

		foreach (CardModel card in selected)
		{
			CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		}

		return Task.CompletedTask;
	}
}

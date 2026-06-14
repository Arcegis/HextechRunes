namespace HextechRunes;

internal sealed class MindOverMatterEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MindOverMatter;

	internal override async Task AfterPlayerTurnStartLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		List<CardModel> pool = player.Character.CardPool
			.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity is not CardRarity.Basic and not CardRarity.Ancient && card.CanBeGeneratedInCombat)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (pool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			pool,
			(RunState)context.RunState,
			HextechStableRandom.CardKey,
			"enemy-mind-over-matter",
			HextechStableRandom.PlayerKey(player),
			combatState.RoundNumber.ToString(),
			CountPlayerDrawnCardsFromHistory(player).ToString());
		CardModel card = combatState.CreateCard(canonicalCard, player);
		if (!card.EnergyCost.CostsX)
		{
			card.EnergyCost.SetUntilPlayed(card.EnergyCost.GetAmountToSpend() + 1, reduceOnly: false);
		}

		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: false);
	}

	private static int CountPlayerDrawnCardsFromHistory(Player player)
	{
		return CombatManager.Instance.History.Entries
			.OfType<CardDrawnEntry>()
			.Count(entry => entry.Card.Owner?.NetId == player.NetId);
	}
}

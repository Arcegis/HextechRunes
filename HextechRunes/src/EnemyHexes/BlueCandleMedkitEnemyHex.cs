namespace HextechRunes;

internal sealed class BlueCandleMedkitEnemyHex : HextechEnemyHexEffect
{
	private static readonly Lazy<IReadOnlyList<CardModel>> CursePool = new(BuildCursePool);

	internal override MonsterHexKind Kind => MonsterHexKind.BlueCandleMedkit;

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		if (room.CombatState is not HextechCombatState combatState || CursePool.Value.Count == 0)
		{
			return;
		}

		foreach (Player player in players
			.Select(static creature => creature.Player)
			.OfType<Player>()
			.OrderBy(static player => player.NetId))
		{
			CardModel canonical = HextechStableRandom.Pick(
				CursePool.Value,
				context.RunState,
				HextechStableRandom.CardKey,
				"blue-candle-medkit-curse",
				HextechStableRandom.PlayerKey(player));
			CardModel curse = combatState.CreateCard(canonical, player);
			await HextechCardGeneration.AddGeneratedCardToCombat(
				curse,
				PileType.Discard,
				addedByPlayer: false,
				CardPilePosition.Top);
		}
	}

	private static IReadOnlyList<CardModel> BuildCursePool()
	{
		return ModelDb.AllCards
			.Where(static card => card.Type == CardType.Curse && card.CanBeGeneratedByModifiers)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
	}
}

namespace HextechRunes;

internal sealed class EightPennyGateEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.EightPennyGate;

	internal override (PileType, CardPilePosition)? ModifyCardPlayResultPileTypeAndPosition(
		HextechEnemyHexContext context,
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (isAutoPlay
			|| card.Type == CardType.Power
			|| card.Owner?.Creature.Side != CombatSide.Player
			|| card.Owner.Creature.CombatState?.RunState != context.RunState)
		{
			return null;
		}

		ulong playerId = card.Owner.NetId;
		if (context.Tracking.EightPennyGatePlayersTriggeredThisTurn.Add(playerId))
		{
			return (PileType.Exhaust, position);
		}

		int limit = context.TierValue(Kind, 1, 1, 2);
		return limit > 1 && context.Tracking.EightPennyGatePlayersTriggeredSecondThisTurn.Add(playerId)
			? (PileType.Exhaust, position)
			: null;
	}
}

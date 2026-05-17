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
		return context.Tracking.EightPennyGatePlayersTriggeredThisTurn.Add(playerId)
			? (PileType.Exhaust, position)
			: null;
	}
}

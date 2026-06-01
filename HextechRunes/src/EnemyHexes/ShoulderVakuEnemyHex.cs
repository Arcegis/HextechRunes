namespace HextechRunes;

internal sealed class ShoulderVakuEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ShoulderVaku;

#if STS2_104_OR_NEWER
	internal override Task AfterAutoPrePlayPhaseEnteredLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
#else
	internal override Task BeforePlayPhaseStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
#endif
	{
		return TryControlSecondTurn(context, player);
	}

	private static async Task TryControlSecondTurn(HextechEnemyHexContext context, Player player)
	{
		if (player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState
			|| combatState.RoundNumber != 2
			|| !context.Tracking.VakuuControlledPlayersThisCombat.Add(player.NetId))
		{
			return;
		}

		int cardsPlayed = await VakuuTurnController.AutoPlayPlayableHand(player);
		VakuuTurnController.PlayLineIfCardsPlayed(player, cardsPlayed);
	}
}

namespace HextechRunes;

internal sealed class IGripEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.IGrip;

	internal override Task AfterCardPlayed(
		HextechEnemyHexContext context,
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (!context.IsManualPlayerCardPlay(cardPlay, out Player? owner, out _)
			|| owner.Creature.IsDead
			|| owner.PlayerCombatState is not { } playerCombatState)
		{
			return Task.CompletedTask;
		}

		int amount = context.TierValue(Kind, 0, 1, 2);
		if (TryConsumeFirstCard(context.Tracking, owner.NetId, amount))
		{
			playerCombatState.LoseEnergy(amount);
		}

		return Task.CompletedTask;
	}

	internal static bool TryConsumeFirstCard(
		HextechMayhemCombatTrackingState tracking,
		ulong playerId,
		int amount)
	{
		return amount > 0 && tracking.GripPlayersTriggeredThisTurn.Add(playerId);
	}
}

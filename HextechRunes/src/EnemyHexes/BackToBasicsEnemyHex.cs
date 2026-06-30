namespace HextechRunes;

internal sealed class BackToBasicsEnemyHex : HextechEnemyHexEffect
{
	// 每回合允许打出的牌数上限(强度档 1/2/3 → 12/10/8)。超过即不可再打出。
	internal const int TurnCardLimitTier1 = 12;
	internal const int TurnCardLimitTier2 = 10;
	internal const int TurnCardLimitTier3 = 8;

	internal override MonsterHexKind Kind => MonsterHexKind.BackToBasics;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player? owner = cardPlay.Card.Owner;
		if (cardPlay.IsAutoPlay
			|| !cardPlay.IsFirstInSeries
			|| owner?.Creature.Side != CombatSide.Player
			|| owner.Creature.CombatState?.RunState != context.RunState
			|| owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Dictionary<ulong, int> played = context.Tracking.BackToBasicsCardsPlayedThisTurn;
		played[owner.NetId] = played.GetValueOrDefault(owner.NetId) + 1;
		return Task.CompletedTask;
	}

	internal static int GetTurnCardLimit(HextechMayhemModifier modifier)
	{
		return new HextechEnemyHexContext(modifier)
			.TierValue(MonsterHexKind.BackToBasics, TurnCardLimitTier1, TurnCardLimitTier2, TurnCardLimitTier3);
	}
}

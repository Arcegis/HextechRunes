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
		if (!context.IsManualPlayerCardPlay(cardPlay, out Player? owner, out _)
			|| owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Dictionary<ulong, int> played = context.Tracking.BackToBasicsCardsPlayedThisTurn;
		played[owner.NetId] = played.GetValueOrDefault(owner.NetId) + 1;
		return Task.CompletedTask;
	}

	// 达到本回合上限后其余牌不可再打出;只管玩家的手动出牌,自动打出不计也不拦。
	internal override bool ShouldPlay(HextechEnemyHexContext context, CardModel card, AutoPlayType autoPlayType)
	{
		// 分发层已限定本局战斗中的玩家侧卡牌。
		if (autoPlayType != AutoPlayType.None || card.Owner is not Player owner)
		{
			return true;
		}

		int limit = context.TierValue(MonsterHexKind.BackToBasics, TurnCardLimitTier1, TurnCardLimitTier2, TurnCardLimitTier3);
		return context.Tracking.BackToBasicsCardsPlayedThisTurn.GetValueOrDefault(owner.NetId) < limit;
	}
}

namespace HextechRunes;

internal sealed class EndlessRotationEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.EndlessRotation;

	internal override Task AfterShuffle(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player shuffler)
	{
		foreach (CardModel card in PileType.Hand.GetPile(shuffler).Cards)
		{
			if (!card.EnergyCost.CostsX)
			{
				// 整回合叠加，打出再返回手牌不清除；由原版回合结束清理。
				card.EnergyCost.AddThisTurn(1);
				HextechPresentation.TryRun("EndlessRotation", "Cost visual refresh failed", card.InvokeEnergyCostChanged);
			}
		}

		return Task.CompletedTask;
	}
}

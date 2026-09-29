namespace HextechRunes;

internal static class HextechAttackCostPreviewRefresher
{
	public static void Refresh(
		HextechMayhemModifier modifier,
		RunState runState,
		IEnumerable<Creature> playerCreatures)
	{
		if (!HextechEnemyHexEffects.HasActiveAttackCostPreviewEffect(modifier))
		{
			return;
		}

		foreach (Creature playerCreature in playerCreatures)
		{
			Player? player = playerCreature.Player;
			if (player == null
				|| playerCreature.CombatState?.RunState != runState)
			{
				continue;
			}

			foreach (CardModel card in PileType.Hand.GetPile(player).Cards)
			{
				if (HextechCardEffectTypes.IsAttackForEffects(card, player) && !card.EnergyCost.CostsX)
				{
					HextechPresentation.TryRun("AttackCostPreview", $"Cost visual refresh failed for {card.Id}", card.InvokeEnergyCostChanged);
				}
			}
		}
	}
}

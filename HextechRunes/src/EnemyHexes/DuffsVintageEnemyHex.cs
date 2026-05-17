namespace HextechRunes;

internal sealed class DuffsVintageEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DuffsVintage;

	internal override bool ShouldFlush(HextechEnemyHexContext context, Player player)
	{
		return player.Creature.CombatState?.RunState != context.RunState;
	}

	internal override Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		if (side != CombatSide.Player || combatRoom == null)
		{
			return Task.CompletedTask;
		}

		foreach (Creature playerCreature in context.GetAlivePlayerSideCreatures(combatRoom.CombatState))
		{
			Player? player = playerCreature.Player;
			if (player == null)
			{
				continue;
			}

			foreach (CardModel card in PileType.Hand.GetPile(player).Cards)
			{
				if (!card.EnergyCost.CostsX)
				{
					card.EnergyCost.SetUntilPlayed(card.EnergyCost.GetAmountToSpend() + 1, reduceOnly: false);
				}
			}
		}

		return Task.CompletedTask;
	}
}

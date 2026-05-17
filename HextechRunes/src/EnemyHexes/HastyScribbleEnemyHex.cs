namespace HextechRunes;

internal sealed class HastyScribbleEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HastyScribble;

	internal override async Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		if (side != CombatSide.Player || combatRoom == null)
		{
			return;
		}

		foreach (Creature playerCreature in context.GetAlivePlayerSideCreatures(combatRoom.CombatState))
		{
			Player? player = playerCreature.Player;
			if (player == null)
			{
				continue;
			}

			int handCount = PileType.Hand.GetPile(player).Cards.Count;
			if (handCount > 0)
			{
				await CreatureCmd.Damage(choiceContext, playerCreature, handCount, ValueProp.Unpowered, null, null);
			}
		}
	}
}

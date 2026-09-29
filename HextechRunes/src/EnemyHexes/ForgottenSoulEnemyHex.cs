namespace HextechRunes;

internal sealed class ForgottenSoulEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ForgottenSoul;

	internal override async Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		if (side != CombatSide.Player || combatRoom == null)
		{
			return;
		}

		foreach (Creature playerCreature in context.GetAlivePlayerSideCreaturesTakingTurn(combatRoom.CombatState))
		{
			Player? player = playerCreature.Player;
			if (player == null)
			{
				continue;
			}

			List<CardModel> cards = PileType.Exhaust.GetPile(player).Cards
				.Where(static card => card.Type is CardType.Status or CardType.Curse)
				.ToList();
			if (cards.Count == 0)
			{
				continue;
			}

			await CardPileCmd.Add(cards, PileType.Discard);
		}
	}
}

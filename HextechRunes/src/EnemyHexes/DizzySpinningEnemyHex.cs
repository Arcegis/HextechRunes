namespace HextechRunes;

internal sealed class DizzySpinningEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DizzySpinning;

	internal override async Task AfterShuffle(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler.Creature.Side != CombatSide.Player
			|| shuffler.Creature.IsDead
			|| shuffler.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		int dazedCount = context.TierValue(Kind, 1, 1, 2);
		for (int i = 0; i < dazedCount; i++)
		{
			CardModel dazed = combatState.CreateCard<Dazed>(shuffler);
			await HextechCardGeneration.AddGeneratedCardToCombat(
				dazed,
				PileType.Draw,
				addedByPlayer: false,
				CardPilePosition.Random);
		}
	}
}

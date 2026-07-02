namespace HextechRunes;

internal sealed class PhrogParasiteEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.PhrogParasite;

	internal override async Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		if (result.UnblockedDamage <= 0
			|| target.Side != CombatSide.Player
			|| target.Player is not { } player
			|| player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		CardModel infection = combatState.CreateCard<Infection>(player);
		await HextechCardGeneration.AddGeneratedCardToCombat(
			infection,
			PileType.Discard,
			addedByPlayer: false,
			CardPilePosition.Top);
	}
}

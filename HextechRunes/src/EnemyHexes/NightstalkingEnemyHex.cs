namespace HextechRunes;

internal sealed class NightstalkingEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Nightstalking;

	internal override async Task AfterDeath(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Creature target, HextechCombatState combatState)
	{
		IReadOnlyList<Creature> enemies = context.GetAliveEnemies(combatState)
			.Where(enemy => enemy != target)
			.ToList();
		if (enemies.Count > 0)
		{
			await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
			await PowerCmd.Apply<PaperCutsPower>(enemies, 1m, null, null);
		}
	}
}

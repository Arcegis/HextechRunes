namespace HextechRunes;

internal sealed class OminousPactEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.OminousPact;

	internal override Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		if (result.UnblockedDamage <= 0 || target.Side != CombatSide.Player)
		{
			return Task.CompletedTask;
		}

		int doom = Math.Min(result.UnblockedDamage, 999999999);
		return doom > 0
			? PowerCmd.Apply<DoomPower>(target, doom, dealer, cardSource)
			: Task.CompletedTask;
	}
}

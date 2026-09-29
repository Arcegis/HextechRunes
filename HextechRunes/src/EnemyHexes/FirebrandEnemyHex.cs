namespace HextechRunes;

internal sealed class FirebrandEnemyHex : HextechEnemyHexEffect
{
	private const decimal BurnStacks = 2m;

	internal override MonsterHexKind Kind => MonsterHexKind.Firebrand;

	internal override async Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		if (result.UnblockedDamage > 0
			&& target.Side == CombatSide.Player
			&& !HextechBurnPower.IsResolvingDamage)
		{
			await PowerCmd.Apply<HextechBurnPower>(target, BurnStacks, dealer, cardSource);
		}
	}
}

namespace HextechRunes;

internal sealed class ShrinkRayEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ShrinkRay;

	internal override async Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		if (result.UnblockedDamage > 0 && target.Side == CombatSide.Player)
		{
			await PowerCmd.Apply<ShrinkPower>(target, HextechMayhemModifier.ShrinkRayStacks, dealer, cardSource);
		}
	}
}

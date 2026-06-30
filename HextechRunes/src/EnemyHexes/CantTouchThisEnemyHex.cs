namespace HextechRunes;

internal sealed class CantTouchThisEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.CantTouchThis;

	internal override async Task AfterEnemyDamageGivenPlayerHit(HextechEnemyHexContext context, Creature dealer, Creature target)
	{
		if (dealer.IsAlive)
		{
			await HextechEnemyPowerScalingHooks.Apply<BufferPower>(dealer, HextechMayhemModifier.CantTouchThisBufferStacks, dealer, null);
		}
	}
}

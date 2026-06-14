namespace HextechRunes;

internal sealed class ClownCollegeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ClownCollege;

	internal override async Task AfterEnemyDamageReceived(HextechEnemyHexContext context, Creature target, uint combatId, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		if (target.IsAlive && HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.ClownCollegeProcsThisTurn, target, 1))
		{
			await HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(target, HextechMayhemModifier.ClownCollegeSlipperyStacks, target, null);
		}
	}
}

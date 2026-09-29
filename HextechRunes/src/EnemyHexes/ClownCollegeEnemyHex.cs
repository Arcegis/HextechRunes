namespace HextechRunes;

internal sealed class ClownCollegeEnemyHex : HextechEnemyHexEffect
{
	private const decimal SlipperyStacks = 1m;

	internal override MonsterHexKind Kind => MonsterHexKind.ClownCollege;

	internal override async Task AfterEnemyDamageReceived(HextechEnemyHexContext context, Creature target, uint combatId, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		if (target.IsAlive && HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.ClownCollegeProcsThisTurn, target, 1))
		{
			await HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(target, SlipperyStacks, target, null);
		}
	}
}

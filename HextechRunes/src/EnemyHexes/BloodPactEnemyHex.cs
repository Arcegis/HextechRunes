namespace HextechRunes;

internal sealed class BloodPactEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BloodPact;

	internal override async Task AfterEnemyDamageReceived(HextechEnemyHexContext context, Creature target, uint combatId, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		if (target.IsAlive && HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.BloodPactProcsThisTurn, target, 2))
		{
			await PowerCmd.Apply<HextechBloodPactTemporaryStrengthPower>(target, HextechMayhemModifier.BloodPactTemporaryStrengthStacks, target, null);
		}
	}
}

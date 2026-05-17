namespace HextechRunes;

internal sealed class ServantMasterEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ServantMaster;

	internal override int PersistentOrder => 120;

	internal override Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		return context.TryApplyServantMasterIllusion(creature, creature, null);
	}

	internal override Task AfterPowerAmountChanged(HextechEnemyHexContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return power is MinionPower && amount > 0m
			? context.TryApplyServantMasterIllusion(power.Owner, applier, cardSource)
			: Task.CompletedTask;
	}
}

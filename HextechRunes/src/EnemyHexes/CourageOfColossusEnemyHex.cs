namespace HextechRunes;

internal sealed class CourageOfColossusEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.CourageOfColossus;

	internal override async Task AfterCourageTrigger(HextechEnemyHexContext context, Creature source)
	{
		if (HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.CourageProcsThisTurn, source, 1))
		{
			int plating = Math.Max(1, (int)Math.Floor(source.MaxHp * HextechMayhemModifier.CourageOfColossusPlatingPercent));
			await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(source, plating, source, null);
		}
	}
}

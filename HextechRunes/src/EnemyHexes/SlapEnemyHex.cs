namespace HextechRunes;

internal sealed class SlapEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Slap;

	internal override async Task AfterMonsterDebuffApplied(HextechEnemyHexContext context, PowerModel power, decimal amount, Creature target, Creature source, CardModel? cardSource)
	{
		if (HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.SlapProcsThisTurn, source, 3))
		{
			await PowerCmd.Apply<StrengthPower>(source, 1m, source, null);
		}
	}
}

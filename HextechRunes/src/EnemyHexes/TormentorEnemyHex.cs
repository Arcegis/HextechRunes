namespace HextechRunes;

internal sealed class TormentorEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Tormentor;

	internal override async Task AfterMonsterDebuffApplied(HextechEnemyHexContext context, PowerModel power, decimal amount, Creature target, Creature source, CardModel? cardSource)
	{
		if (context.Tracking.HandlingMonsterTormentorBurn
			|| !HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.TormentorProcsThisTurn, source, 5))
		{
			return;
		}

		try
		{
			context.Tracking.HandlingMonsterTormentorBurn = true;
			await PowerCmd.Apply<HextechBurnPower>(target, 2m, source, null);
		}
		finally
		{
			context.Tracking.HandlingMonsterTormentorBurn = false;
		}
	}
}

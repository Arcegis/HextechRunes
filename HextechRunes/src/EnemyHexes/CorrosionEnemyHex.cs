namespace HextechRunes;

internal sealed class CorrosionEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Corrosion;

	internal override async Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		if (result.UnblockedDamage <= 0 || target.Player == null)
		{
			return;
		}

		// 每个敌人每回合首次对玩家造成未格挡伤害时触发一次。
		if (!HextechCombatProcTracker.TryConsumeLimitedProc(context.Tracking.CorrosionProcsThisTurn, dealer, 1))
		{
			return;
		}

		bool loseStrength = HextechStableRandom.PercentChance(
			context.RunState,
			50,
			"enemy-corrosion",
			HextechStableRandom.PlayerKey(target.Player),
			dealer.CombatId?.ToString() ?? "0",
			target.CombatState?.RoundNumber.ToString() ?? "-1");
		if (loseStrength)
		{
			await PowerCmd.Apply<StrengthPower>(target, -1m, dealer, null);
		}
		else
		{
			await PowerCmd.Apply<DexterityPower>(target, -1m, dealer, null);
		}
	}
}

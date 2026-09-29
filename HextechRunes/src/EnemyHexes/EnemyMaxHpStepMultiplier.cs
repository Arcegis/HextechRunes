namespace HextechRunes;

/// <summary>
/// 重拳出击、活力涌动、蛋白奶昔共用的倍率：敌人最大生命每满（每人基数 × 玩家数）点，倍率 +1%，可选百分比上限。
/// 每人基数同时写在 MonsterHexCatalog.PlayerCountScaledThresholds 的字面量里（TXT 同步脚本按字面量渲染），测试断言两处一致。
/// </summary>
internal static class EnemyMaxHpStepMultiplier
{
	/// <param name="playerCount">取 <see cref="HextechEnemyHexContext.ScalingPlayerCount"/>，已夹到 1..16。</param>
	internal static decimal Resolve(decimal maxHp, int hpPerPercentPerPlayer, int playerCount, int? maxBonusPercent = null)
	{
		decimal hpPerPercent = (decimal)hpPerPercentPerPlayer * playerCount;
		decimal bonusPercent = Math.Max(0m, Math.Floor(maxHp / hpPerPercent));
		if (maxBonusPercent is int cap)
		{
			bonusPercent = Math.Min(cap, bonusPercent);
		}

		return 1m + bonusPercent / 100m;
	}
}

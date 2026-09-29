namespace HextechRunes;

internal static class HextechCreatureStatLimits
{
	// 原版生命、最大生命、格挡的统一硬上限：Creature.LoseHpInternal 的 Math.Clamp(amount, 0m, 999999999m)、
	// SetMaxHpInternal 与 GainBlockInternal 的 Math.Min(..., 999999999)（0.107.1 / 0.110.0 / 0.111.0 相同）。
	internal const int StatHardCap = 999999999;
}

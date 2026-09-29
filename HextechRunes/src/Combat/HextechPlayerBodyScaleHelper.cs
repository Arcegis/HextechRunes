using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

internal static class HextechPlayerBodyScaleHelper
{
	// 生物体型缩放下限（体型再小就看不清了）；玩家与敌方（HextechMonsterMaxHpCoefficients.UpdateEnemyScale）共用。
	internal const float MinCreatureBodyScale = 0.2f;

	internal static void Update(Player? player)
	{
		if (player == null)
		{
			return;
		}

		float scale = 1f;
		scale += player.GetRelic<GoliathRune>()?.BodyScaleDelta ?? 0f;
		scale += player.GetRelic<GiantSlayerRune>()?.BodyScaleDelta ?? 0f;
		scale += player.GetRelic<TankEngineRune>()?.BodyScaleDelta ?? 0f;
		scale += player.GetRelic<ShrinkEngineRune>()?.BodyScaleDelta ?? 0f;
		scale += player.GetRelic<NineDragonPowerRune>()?.BodyScaleDelta ?? 0f;

		try
		{
			NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.SetDefaultScaleTo(Math.Max(MinCreatureBodyScale, scale), 0f);
		}
		catch (Exception ex)
		{
			HextechLog.Warn("BodyScale", $"Creature visual failed: {ex.Message}");
		}
	}
}

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private void ResetCombatTracking()
	{
		HextechEnemyHexEffects.ResetAllRunScopedState();
		_runContext.ResetCombatTracking();
		// 出牌被打断时 AfterCardPlayedLate 不会来取，残留条目会跨战斗持有卡牌引用。
		_stormLightningAtCardStart.Clear();
	}
}

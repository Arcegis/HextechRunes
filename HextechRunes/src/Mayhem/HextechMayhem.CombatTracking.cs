namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	internal HextechMayhemCombatTrackingState CombatTracking => _runContext.CombatTracking;

	private void ResetCombatTracking()
	{
		HextechEnemyHexEffects.ResetAllRunScopedState();
		_runContext.ResetCombatTracking();
	}
}

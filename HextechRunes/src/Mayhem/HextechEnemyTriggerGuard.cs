using System.Globalization;

namespace HextechRunes;

internal static class HextechEnemyTriggerGuard
{
	public static bool ShouldSuppressDuplicateEnemyThresholdTrigger(
		HextechMayhemCombatTrackingState tracking,
		Creature target,
		DamageResult result,
		Creature? dealer,
		CardModel? cardSource)
	{
		string key = string.Join(":",
			target.CombatId?.ToString() ?? "none",
			target.CurrentHp.ToString(CultureInfo.InvariantCulture),
			result.UnblockedDamage.ToString(CultureInfo.InvariantCulture),
			dealer?.CombatId?.ToString() ?? "none",
			HextechStableRandom.CardActionKey(cardSource));
		bool suppress = key == tracking.LastEnemyThresholdTriggerKey;
		tracking.LastEnemyThresholdTriggerKey = key;
		return suppress;
	}
}

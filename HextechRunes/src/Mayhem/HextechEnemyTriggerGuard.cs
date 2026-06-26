using System.Globalization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal static class HextechEnemyTriggerGuard
{
	public static async Task RunGroupedPlayerDebuffBurst(HextechMayhemCombatTrackingState tracking, Func<Task> action)
	{
		bool wasHandlingGroupedPlayerDebuffs = tracking.HandlingGroupedPlayerDebuffs;
		if (!wasHandlingGroupedPlayerDebuffs)
		{
			tracking.HandlingGroupedPlayerDebuffs = true;
			tracking.GroupedPlayerDebuffProcKeys.Clear();
		}

		try
		{
			await action();
		}
		finally
		{
			if (!wasHandlingGroupedPlayerDebuffs)
			{
				tracking.GroupedPlayerDebuffProcKeys.Clear();
				tracking.HandlingGroupedPlayerDebuffs = false;
			}
		}
	}

	public static bool ShouldSuppressMonsterDebuffDuplicate(
		HextechMayhemCombatTrackingState tracking,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		string powerTypeName = power.GetType().FullName ?? power.GetType().Name;
		if (tracking.HandlingGroupedPlayerDebuffs)
		{
			string groupedKey = $"{applier?.CombatId?.ToString() ?? "none"}:{powerTypeName}:{amount}";
			return !tracking.GroupedPlayerDebuffProcKeys.Add(groupedKey);
		}

		if (cardSource == null || applier?.CombatId == null)
		{
			return false;
		}

		string actionKey = $"{applier.CombatId.Value}:{HextechStableRandom.CardActionKey(cardSource)}:{powerTypeName}:{amount}";
		return !tracking.MonsterDebuffActionProcKeysThisTurn.Add(actionKey);
	}

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

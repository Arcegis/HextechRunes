using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	// 记录每次伤害命令里,哪些玩家滑溜「减伤生效了」(在 ModifyHpLostAfterOsty 把 >=1 的伤害压到 1)。
	// 原版滑溜的消耗只看 result.UnblockedDamage>=1,但钨合金棍等会在滑溜之后继续 ModifyHpLostAfterOsty
	// 把这 1 点伤害再减到 0,导致 UnblockedDamage=0 → 滑溜「免了伤却不掉层」→ 永久无敌。
	// 这里追踪滑溜是否真减伤,在它减伤却没被原版消耗时补一次消耗。
	private static readonly Dictionary<long, HashSet<SlipperyPower>> SlipperyReductionsByCommand = new();

	private static void SlipperyModifyHpLostAfterOstyPostfix(SlipperyPower __instance, Creature target, decimal amount, ref decimal __result)
	{
		// target!=Owner(如伤害分摊给 Osty)或伤害本就 <1、或滑溜没把它压低,都不算「滑溜减伤」。
		if (target != __instance.Owner || amount < 1m || __result >= amount)
		{
			return;
		}

		long commandId = CurrentActualDamageCommandId;
		if (commandId == 0L)
		{
			return;
		}

		if (!SlipperyReductionsByCommand.TryGetValue(commandId, out HashSet<SlipperyPower>? reduced))
		{
			reduced = [];
			SlipperyReductionsByCommand[commandId] = reduced;
		}

		reduced.Add(__instance);
	}

	private static void SlipperyAfterDamageReceivedPostfix(SlipperyPower __instance, Creature target, DamageResult result, ref Task __result)
	{
		// 原版在 result.UnblockedDamage>=1 时已自行消耗;这里只补「减伤生效但最终伤害被压到 <1」的漏网情形。
		if (target != __instance.Owner || result.UnblockedDamage >= 1)
		{
			return;
		}

		long commandId = CurrentActualDamageCommandId;
		if (commandId == 0L
			|| !SlipperyReductionsByCommand.TryGetValue(commandId, out HashSet<SlipperyPower>? reduced)
			|| !reduced.Remove(__instance))
		{
			return;
		}

		__result = AppendSlipperyConsumption(__result, __instance);
	}

	private static async Task AppendSlipperyConsumption(Task original, SlipperyPower power)
	{
		await original;
		await PowerCmd.Decrement(power);
	}

	private static void ClearSlipperyReductions(long commandId)
	{
		SlipperyReductionsByCommand.Remove(commandId);
	}
}

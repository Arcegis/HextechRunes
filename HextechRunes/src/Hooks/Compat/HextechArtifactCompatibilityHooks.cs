namespace HextechRunes;

internal static class HextechArtifactCompatibilityHooks
{
	private static bool IsEncounterMechanicPower(PowerModel power)
	{
		return power is SurroundedPower;
	}

	// 原版缩小正层数是逐回合递减的临时缩小，ShrinkPower.AfterSideTurnEnd 用 PowerCmd.Decrement 扣 1 层。
	// 缩小允许负层数，原版 GetTypeForAmount(-1) 把这次递减判成负面效果，人工制品会挡掉它并消耗一层：
	// 临时缩小永不结束，还每回合吃掉一层人工制品。只放行"已有正层数实例、无外部施加者"的到期递减；
	// 敌人对临时缩小再施加永久缩小（施加者是敌人）仍按负面效果处理。
	internal static bool IsTemporaryShrinkTickDown(PowerModel power, Creature target, decimal amount, Creature? applier)
	{
		return power is ShrinkPower
			&& amount < 0m
			&& (applier == null || applier == target)
			&& target.Powers.Contains(power)
			&& power.Amount > 0;
	}

	// 本模组的临时力量/敏捷丢失是隐藏能力，原版人工制品只挡可见能力，于是放行外壳、只挡住
	// BeforeApplied 里施加的负力量/负敏捷；回合末外壳照常回收，目标凭空多出永久属性。
	// 这里按可见负面效果处理，由人工制品整体抵挡。
	internal static bool IsHiddenTemporaryStatLoss(PowerModel power, decimal amount)
	{
		return amount > 0m
			&& power is HextechTemporaryStrengthLossPower or HextechTemporaryDexterityLossPower;
	}

	[HarmonyPatch(typeof(ArtifactPower), nameof(ArtifactPower.TryModifyPowerAmountReceived), new[] { typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(decimal) }, new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
	[HextechPatch("compat.artifact", "人工制品遭遇战、缩小递减与隐藏临时减益兼容")]
	private static class TryModifyPatch
	{
		// 跳过型前缀（已裁决保留，见 architecture.md）：人工制品的判定没有可挂的 Hook，只能在原方法前给出结果。
		// 第四个参数在原版里名为 "_"，按位置 __3 绑定。
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(
			ArtifactPower __instance,
			PowerModel canonicalPower,
			Creature target,
			decimal amount,
			Creature? __3,
			ref decimal modifiedAmount,
			ref bool __result)
		{
			if (target != __instance.Owner)
			{
				return true;
			}

			if (IsHiddenTemporaryStatLoss(canonicalPower, amount))
			{
				modifiedAmount = 0m;
				__result = true;
				return false;
			}

			if ((!IsEncounterMechanicPower(canonicalPower) && !IsTemporaryShrinkTickDown(canonicalPower, target, amount, __3))
				|| !HextechCombatHooks.IsVanillaFixActiveFor(__instance.Owner))
			{
				return true;
			}

			modifiedAmount = amount;
			__result = false;
			return false;
		}
	}
}

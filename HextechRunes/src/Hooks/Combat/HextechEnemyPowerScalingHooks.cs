namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	private enum ScalingOverride
	{
		Unscaled,
		PlayerCount
	}

	// 本模组 ApplyExact 的施加窗口：层数已按最终口径算好，窗口内跳过原版对敌方的联机缩放。
	private static readonly AsyncLocal<bool> InExactApply = new();

	internal static async Task<T?> Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
		where T : PowerModel
	{
		if (GetScalingOverride(typeof(T)) is not ScalingOverride scalingOverride)
		{
			return await PowerCmd.Apply<T>(target, amount, applier, cardSource, silent);
		}

		decimal finalAmount = CalculateFinalAmount(target, amount, applier, scalingOverride);
		return await ApplyExact<T>(target, finalAmount, applier, cardSource, silent);
	}

	/// <summary>
	/// 按原值应用,绕过原版联机缩放。原版 PowerCmd.Apply 对敌方目标且 ShouldScaleInMultiplayer
	/// 的 power(Slippery/Artifact 等)会自动 ×玩家数;层数已按最终口径算好的调用方(墨影幻灵)走这里。
	/// </summary>
	internal static async Task<T?> ApplyExact<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
		where T : PowerModel
	{
		decimal finalAmount = ClampPowerOffsetForApply<T>(target, amount);
		if (finalAmount == 0m)
		{
			return target.GetPower<T>();
		}

		Creature? effectiveApplier = ShouldClearSelfApplier(target, applier) ? null : applier;
		bool previous = InExactApply.Value;
		InExactApply.Value = true;
		try
		{
			return await PowerCmd.Apply<T>(target, finalAmount, effectiveApplier, cardSource, silent);
		}
		finally
		{
			InExactApply.Value = previous;
		}
	}

	private static bool GetScaledAmountForMultiplayerPrefix(
		PowerModel __instance,
		decimal amount,
		Creature target,
		ref decimal __result)
	{
		if (!InExactApply.Value
			|| target == null
			|| (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
			|| GetScalingOverride(__instance.GetType()) == null)
		{
			return true;
		}

		__result = ClampPowerOffsetForApply(__instance, target, amount);
		return false;
	}

	private static decimal CalculateFinalAmount(Creature target, decimal amount, Creature? applier, ScalingOverride scalingOverride)
	{
		if (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
		{
			return amount;
		}

		return scalingOverride == ScalingOverride.PlayerCount
			? MultiplyByPlayerCount(amount, GetPlayerCount(applier, target))
			: ClampPowerAmount(amount);
	}

	private static decimal ClampPowerOffsetForApply<T>(Creature target, decimal amount)
		where T : PowerModel
	{
		return ClampPowerOffsetForApply(ModelDb.Power<T>(), target, amount);
	}

	private static decimal ClampPowerOffsetForApply(PowerModel power, Creature target, decimal amount)
	{
		decimal clamped = ClampPowerAmount(amount);
		if (IsInstancedPower(power))
		{
			return clamped;
		}

		int currentAmount = target.GetPower(power.Id)?.Amount ?? 0;
		if (clamped > 0m)
		{
			decimal maxOffset = int.MaxValue - (decimal)currentAmount;
			return Math.Min(clamped, Math.Max(0m, maxOffset));
		}

		if (clamped < 0m)
		{
			decimal minOffset = int.MinValue - (decimal)currentAmount;
			return Math.Max(clamped, Math.Min(0m, minOffset));
		}

		return clamped;
	}

	private static bool IsInstancedPower(PowerModel power)
	{
		return power.InstanceType != PowerInstanceType.None;
	}

	private static bool ShouldClearSelfApplier(Creature target, Creature? applier)
	{
		return applier != null
			&& ReferenceEquals(target, applier)
			&& (target.IsPrimaryEnemy || target.IsSecondaryEnemy);
	}

	// 跳过型前缀用 Priority.First（已裁决保留，见 architecture.md）：只在本模组 Apply/ApplyExact 的 AsyncLocal
	// 窗口内（InExactApply）且目标是敌人时生效，窗口外恒 return true、对其他模组透明；
	// 窗口内层数已按最终口径算好，若排在第三方缩放前缀之后，会被它们再缩放一次或被它们的跳过前缀挡掉。
	// 目标由 ScalingOverrides 各类型的 GetScaledAmountForMultiplayer 声明处派生（需运行时去重，故动态安装）。
	[HextechPatch("combat.enemy-power-scaling", "敌方能力联机缩放")]
	private static class ScaledAmountPatch
	{
		private static void Apply(Harmony harmony)
		{
			List<MethodInfo> targets = ResolveGetScaledAmountForMultiplayerTargets();
			if (targets.Count == 0)
			{
				// 抛给 HextechPatcher 统一记失败，不能静默当作已安装。
				throw new InvalidOperationException("GetScaledAmountForMultiplayer targets not found in this runtime.");
			}

			HarmonyMethod scaledPrefix = new(typeof(HextechEnemyPowerScalingHooks), nameof(GetScaledAmountForMultiplayerPrefix))
			{
				priority = Priority.First
			};

			foreach (MethodInfo scaledTarget in targets)
			{
				harmony.Patch(scaledTarget, prefix: scaledPrefix);
			}
		}
	}
}

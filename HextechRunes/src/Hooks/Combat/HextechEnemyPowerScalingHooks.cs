using HarmonyLib;

namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	private enum ScalingOverride
	{
		Unscaled,
		PlayerCount,
		FinalAmount
	}

	private static readonly AsyncLocal<ScalingOverride?> CurrentOverride = new();

	public static void Install(Harmony harmony)
	{
#if STS2_107_OR_NEWER
		HarmonyMethod prefix = new(typeof(HextechEnemyPowerScalingHooks), nameof(ModifyPowerAmountGivenHookPrefix))
		{
			priority = Priority.First
		};
#else
		HarmonyMethod prefix = new(typeof(HextechEnemyPowerScalingHooks), nameof(ModifyPowerAmountGivenPrefix))
		{
			priority = Priority.First
		};
#endif

		MethodInfo? modifyPowerAmountGivenTarget = TryResolveModifyPowerAmountGivenTarget();
		if (modifyPowerAmountGivenTarget != null)
		{
			harmony.Patch(modifyPowerAmountGivenTarget, prefix: prefix);
		}
		else
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem][Compat] Enemy power multiplayer scaling hook skipped: ModifyPowerAmountGiven target not found in this runtime.");
		}

#if STS2_105_OR_NEWER
		HarmonyMethod scaledPrefix = new(typeof(HextechEnemyPowerScalingHooks), nameof(GetScaledAmountForMultiplayerPrefix))
		{
			priority = Priority.First
		};

		foreach (MethodInfo scaledTarget in ResolveGetScaledAmountForMultiplayerTargets())
		{
			harmony.Patch(scaledTarget, prefix: scaledPrefix);
		}
#endif
	}

	public static async Task<T?> Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
		where T : PowerModel
	{
		ScalingOverride? scalingOverride = GetScalingOverride(typeof(T));
		if (scalingOverride == null)
		{
			return await PowerCmd.Apply<T>(target, amount, applier, cardSource, silent);
		}

		decimal finalAmount = CalculateFinalAmount(target, amount, applier, scalingOverride.Value);
		finalAmount = ClampPowerOffsetForApply<T>(target, finalAmount);
		if (finalAmount == 0m)
		{
			return target.GetPower<T>();
		}

		Creature? effectiveApplier = ShouldClearSelfApplier(target, applier) ? null : applier;
		using (BeginOverride(ScalingOverride.FinalAmount))
		{
			return await PowerCmd.Apply<T>(target, finalAmount, effectiveApplier, cardSource, silent);
		}
	}

	/// <summary>
	/// 按原值应用,绕过原版联机缩放。原版 PowerCmd.Apply 对敌方目标且 ShouldScaleInMultiplayer
	/// 的 power(Slippery/Artifact 等)会自动 ×玩家数;层数已按最终口径算好的调用方(墨影幻灵)走这里。
	/// </summary>
	public static async Task<T?> ApplyExact<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
		where T : PowerModel
	{
		decimal finalAmount = ClampPowerOffsetForApply<T>(target, amount);
		if (finalAmount == 0m)
		{
			return target.GetPower<T>();
		}

		Creature? effectiveApplier = ShouldClearSelfApplier(target, applier) ? null : applier;
		using (BeginOverride(ScalingOverride.FinalAmount))
		{
			return await PowerCmd.Apply<T>(target, finalAmount, effectiveApplier, cardSource, silent);
		}
	}

#if STS2_107_OR_NEWER
	private static bool ModifyPowerAmountGivenHookPrefix(
		ICombatState combatState,
		PowerModel power,
		Creature? giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource,
		ref IEnumerable<AbstractModel> modifiers,
		ref decimal __result)
	{
		if (!TryCalculateModifiedPowerAmountGiven(power, giver, amount, target, out decimal modifiedAmount))
		{
			return true;
		}

		modifiers = Array.Empty<AbstractModel>();
		__result = modifiedAmount;
		return false;
	}
#else
	private static bool ModifyPowerAmountGivenPrefix(
		PowerModel power,
		Creature? giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource,
		ref decimal __result)
	{
		if (!TryCalculateModifiedPowerAmountGiven(power, giver, amount, target, out decimal modifiedAmount))
		{
			return true;
		}

		__result = modifiedAmount;
		return false;
	}
#endif

	private static bool TryCalculateModifiedPowerAmountGiven(
		PowerModel power,
		Creature? giver,
		decimal amount,
		Creature? target,
		out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		ScalingOverride? activeOverride = CurrentOverride.Value;
		ScalingOverride? powerOverride = GetScalingOverride(power.GetType());
		if (activeOverride == null
			|| target == null
			|| (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
			|| powerOverride == null
			|| (activeOverride.Value != ScalingOverride.FinalAmount && powerOverride != activeOverride))
		{
			return false;
		}

		modifiedAmount = activeOverride.Value switch
		{
			ScalingOverride.PlayerCount => ClampPowerOffsetForApply(power, target, MultiplyByPlayerCount(amount, GetPlayerCount(giver, target))),
			ScalingOverride.Unscaled => ClampPowerOffsetForApply(power, target, amount),
			ScalingOverride.FinalAmount => ClampPowerOffsetForApply(power, target, amount),
			_ => ClampPowerOffsetForApply(power, target, amount)
		};
		return true;
	}

#if STS2_105_OR_NEWER
	private static bool GetScaledAmountForMultiplayerPrefix(
		PowerModel __instance,
		decimal amount,
		Creature target,
		ref decimal __result)
	{
		if (CurrentOverride.Value != ScalingOverride.FinalAmount
			|| target == null
			|| (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
			|| GetScalingOverride(__instance.GetType()) == null)
		{
			return true;
		}

		__result = ClampPowerOffsetForApply(__instance, target, amount);
		return false;
	}
#endif

	private static decimal CalculateFinalAmount(Creature target, decimal amount, Creature? applier, ScalingOverride scalingOverride)
	{
		if (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
		{
			return amount;
		}

		return scalingOverride switch
		{
			ScalingOverride.PlayerCount => MultiplyByPlayerCount(amount, GetPlayerCount(applier, target)),
			ScalingOverride.Unscaled => ClampPowerAmount(amount),
			ScalingOverride.FinalAmount => ClampPowerAmount(amount),
			_ => ClampPowerAmount(amount)
		};
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
#if STS2_105_OR_NEWER
		return power.InstanceType != PowerInstanceType.None;
#else
		return power.IsInstanced;
#endif
	}

	private static bool ShouldClearSelfApplier(Creature target, Creature? applier)
	{
		return applier != null
			&& ReferenceEquals(target, applier)
			&& (target.IsPrimaryEnemy || target.IsSecondaryEnemy);
	}

}

using MegaCrit.Sts2.Core.Hooks;

namespace HextechRunes;

/// <summary>
/// 符文内容用到的 0.107.1↔0.108+ 直接调用类 API 差异集中适配，签名统一，调用方不写行内 <c>#if</c>：
/// Hook.ModifyDamage 追加 CardPlay、ITemporaryPower.IgnoreNextInstance 移除、
/// CreatureCmd.LoseBlock 首参追加 PlayerChoiceContext 与移除者、CardPlay 新增 required Player。
/// </summary>
internal static class HextechRuneApiCompat
{
	/// <summary>0.108.0 起 Hook.ModifyDamage 在 cardSource 后追加 CardPlay 参数；此处不关联任何一次打出。</summary>
	internal static decimal ModifyDamage(
		IRunState runState,
		HextechCombatState? combatState,
		Creature? target,
		Creature? dealer,
		decimal damage,
		ValueProp props,
		CardModel? cardSource,
		ModifyDamageHookType modifyDamageHookType,
		CardPreviewMode previewMode)
	{
#if STS2_108_OR_NEWER
		return Hook.ModifyDamage(runState, combatState, target, dealer, damage, props, cardSource, null, modifyDamageHookType, previewMode, out _);
#else
		return Hook.ModifyDamage(runState, combatState, target, dealer, damage, props, cardSource, modifyDamageHookType, previewMode, out _);
#endif
	}

	/// <summary>
	/// 重复施加一个已存在的临时能力前调用。0.107.1 需要 ITemporaryPower.IgnoreNextInstance 让下一次施加不被当成新实例；
	/// 0.108.0 起该接口方法被移除，临时能力的实例追踪由引擎处理，这里什么都不做。
	/// </summary>
	internal static void PrepareTemporaryPowerReapply(PowerModel power)
	{
#if STS2_108_OR_NEWER
		_ = power;
#else
		if (power is ITemporaryPower temporaryPower)
		{
			temporaryPower.IgnoreNextInstance();
		}
#endif
	}

	/// <summary>0.109.0 起 CreatureCmd.LoseBlock 首参新增 PlayerChoiceContext，并追加移除者参数。</summary>
	internal static Task LoseBlock(PlayerChoiceContext choiceContext, Creature target, decimal amount, Creature? remover)
	{
#if STS2_109_OR_NEWER
		return CreatureCmd.LoseBlock(choiceContext, target, amount, remover);
#else
		_ = choiceContext;
		_ = remover;
		return CreatureCmd.LoseBlock(target, amount);
#endif
	}

	/// <summary>
	/// 构造一次不花费资源、结果堆为 None 的自动打出记录。0.109.0 起 CardPlay 新增 required Player(打出者)；
	/// 回放由卡牌所有者打出。
	/// </summary>
	internal static CardPlay CreateFreeAutoPlay(CardModel card, Creature? target)
	{
		return new CardPlay
		{
			Card = card,
#if STS2_109_OR_NEWER
			Player = card.Owner,
#endif
			Target = target,
			ResultPile = PileType.None,
			Resources = new ResourceInfo
			{
				EnergySpent = 0,
				EnergyValue = 0,
				StarsSpent = 0,
				StarValue = 0
			},
			IsAutoPlay = true,
			PlayIndex = 0,
			PlayCount = 1
		};
	}
}

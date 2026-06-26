using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	// 敌方「双刀流」意图预览:让头顶攻击意图和实战保持一致——每段白值减半(向上取整)、段数加倍。
	//
	// 实战由 DualWieldAttackCommandExecutePrefix 改写 AttackCommand 的 _damagePerHit/_hitCount 实现;
	// 但意图是攻击执行前单独算好显示的,不经过 AttackCommand.Execute,所以光改实战意图不会变。
	//
	// 这里在 UI 渲染入口 NIntent.UpdateIntent(intent, targets, owner) 处拦截:当 owner 是敌人且
	// 双刀流生效时,把传进来的 AttackIntent 换成一个等价的「双段」意图(DamageCalc 包一层白值减半,
	// Repeats 翻倍)。NIntent 后续算 label/贴图/动画/hover 全部基于这个新意图,于是单攻也会显示成
	// 「每段伤害 ×段数」,数值与实战一致。
	//
	// 纯无状态转换:每次渲染都基于原始意图的 DamageCalc 重新计算,既不改原意图对象、也不读回自己上次
	// 产出的意图,因此意图反复刷新也不会把伤害越减越少。
	private static void InstallDualWieldIntentHooks(Harmony harmony)
	{
		try
		{
			harmony.Patch(
				RequireMethod(
					typeof(NIntent),
					nameof(NIntent.UpdateIntent),
					BindingFlags.Instance | BindingFlags.Public,
					typeof(AbstractIntent),
					typeof(IEnumerable<Creature>),
					typeof(Creature)),
				prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NIntentUpdateIntentPrefix)));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] 双刀流意图预览 hook 安装失败,意图显示可能与实际伤害不一致: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void NIntentUpdateIntentPrefix(ref AbstractIntent intent, Creature owner)
	{
		// 只处理敌人的攻击意图;已经是我们替换出来的双段意图就别再套娃(防重复减半/加段)。
		if (intent is not AttackIntent attackIntent || attackIntent is DualWieldAttackIntent)
		{
			return;
		}

		if (owner?.Side != CombatSide.Enemy
			|| owner.CombatState?.RunState is not RunState runState
			|| GetMayhemModifier(runState) is not { } modifier
			|| !modifier.HasActiveMonsterHex(MonsterHexKind.DualWield))
		{
			return;
		}

		Func<decimal>? originalDamageCalc = attackIntent.DamageCalc;
		if (originalDamageCalc == null)
		{
			return;
		}

		int doubledRepeats = Math.Max(1, attackIntent.Repeats) * 2;
		intent = new DualWieldAttackIntent(
			() =>
			{
				decimal white = originalDamageCalc();
				// 与 DualWieldAttackCommandExecutePrefix 完全一致:白值 >= 1 才减半(向上取整);
				// 计算型/非正值伤害保持原样,只翻倍段数。减半发生在力量等加成之前(改白值不改系数),
				// GetSingleDamage 随后照常走 Hook.ModifyDamage 叠加加成,于是和实战逐段伤害对得上。
				return white >= 1m ? Math.Ceiling(white / 2m) : white;
			},
			doubledRepeats);
	}
}

/// <summary>
/// 双刀流意图预览专用的「多段攻击意图」:行为等同原版 <c>MultiAttackIntent</c>,但允许直接注入一个
/// 已经做过「白值减半」处理的 <see cref="Func{Decimal}"/> 伤害计算器(原版 MultiAttackIntent 的构造器
/// 只收 int,无法承载动态/减半后的白值)。其余 label/贴图/动画/hover 全部沿用 AttackIntent 基类逻辑。
/// </summary>
internal sealed class DualWieldAttackIntent : AttackIntent
{
	private readonly int _repeats;

	public DualWieldAttackIntent(Func<decimal> damageCalc, int repeats)
	{
		DamageCalc = damageCalc;
		_repeats = repeats;
	}

	public override int Repeats => _repeats;

	protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_MULTI");

	public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
	{
		return GetSingleDamage(targets, owner) * Repeats;
	}

	public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
	{
		LocString format = IntentLabelFormat;
		format.Add("Damage", GetSingleDamage(targets, owner));
		format.Add("Repeat", Repeats);
		return format;
	}
}

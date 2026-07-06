using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

internal static class HextechMonsterInteractionPolicy
{
	public static bool IsTrueCombatDeath(Creature creature)
	{
		return IsTrueCombatDeath(creature, out _);
	}

	public static bool IsTrueCombatDeath(Creature creature, [NotNullWhen(true)] out HextechCombatState? combatState)
	{
		combatState = creature.CombatState;
		return combatState != null
			&& !IsBossPhaseTransitionDeath(creature)
			&& Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);
	}

	public static bool IsBossPhaseTransitionDeath(Creature creature)
	{
		HextechCombatState? combatState = creature.CombatState;
		return combatState != null
			&& creature.Monster is TestSubject
			&& !Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);
	}

	public static bool ShouldIgnoreMonsterSelfBuff(PowerModel power)
	{
		return IsStructuralMonsterBuff(power);
	}

	// 结构性怪物 buff = 驱动怪物动画状态机/回合流转/遭遇脚本/位置关系/意图预告的机制 power。
	// 剥除它们会让原版逻辑断裂(实例:剥幽灵鳗 SkittishPower 后 BlockEnd 动画永不触发,回合流转挂死,
	// 玩家实报"感受燃烧打四鳗卡住无法进入下一回合")。宁可多列——多列的代价只是该增益剥不掉,
	// 漏列的代价是卡死。数值成长类(力量/CreativeAi 等)不在此列,允许正常剥除。
	public static bool IsStructuralMonsterBuff(PowerModel power)
	{
		return power is SandpitPower
			or ReattachPower
			or AdaptablePower
			// 动画/形态状态机
			or SkittishPower
			or SmoggyPower
			or BurrowedPower
			or AsleepPower
#if STS2_108_OR_NEWER
			// 0.108 新增类型
			or HibernatePower
			or SoulboundPower
#endif
			or SlumberPower
			or CurlUpPower
			or HardenedShellPower
			or SentryModePower
			or ShadowmeldPower
			or ShroudPower
			or SneakyPower
			or CoveredPower
			or SurprisePower
			or SoarPower
			or FlutterPower
			or SpectrumShiftPower
			// 遭遇脚本/演出/时限
			or BattlewornDummyTimeLimitPower
			or MonologuePower
			or HatchPower
			or CountdownPower
			or TheSealedThronePower
			or WitheringPresencePower
			or PillarOfCreationPower
			or ChildOfTheStarsPower
			or PaleBlueDotPower
			or TheHuntPower
			or DemesnePower
			or NemesisPower
			or HardToKillPower
			// 位置/关系
			or BackAttackLeftPower
			or BackAttackRightPower
			or UnmovablePower
			or OrbitPower
			or MinionPower
			or GuardedPower
			or InterceptPower
			or DieForYouPower
			or IllusionPower
			or InfestedPower
			or FastenPower
			or HauntPower
			// 意图预告
			or SummonNextTurnPower
			or StarNextTurnPower
			or SteamEruptionPower;
	}
}

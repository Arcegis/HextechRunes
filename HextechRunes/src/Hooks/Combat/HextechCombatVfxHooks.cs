using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HextechRunes;

/// <summary>战斗特效挂在原版节点生命周期上的补丁；特效本体在 <see cref="HextechCombatVfx"/>。</summary>
internal static class HextechCombatVfxHooks
{
	/// <summary>
	/// 吞噬灵魂的特效在死亡动画开始的瞬间派发(真死亡分支必经点),而不是等 rune 的 AfterDeath:
	/// Hook.AfterDeath 是逐监听器顺序 await 的链条,排在前面的监听器等待死亡动画会让魂"卡一下"
	/// 才飞出(最后一只怪死亡时链条提前收尾所以不卡)。此处仅派发表现,数值仍在 rune 内结算。
	/// </summary>
	[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim), typeof(bool))]
	[HextechPatch("visual.combat-vfx.soul-drain", "吞噬灵魂特效")]
	private static class StartDeathAnimPatch
	{
		[HarmonyPostfix]
		private static void Postfix(NCreature __instance) => DispatchSoulDrainOnDeathAnim(__instance);
	}

	private static void DispatchSoulDrainOnDeathAnim(NCreature deadNode)
	{
		try
		{
			Creature? dead = deadNode.Entity;
			if (dead is not { Side: CombatSide.Enemy } || dead.CombatState is not { } combatState
				|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(dead))
			{
				return;
			}

			foreach (Player player in combatState.Players)
			{
				if (player.Creature is { IsDead: false } collector && player.GetRelic<SoulEaterRune>() != null)
				{
					// 缕数必须在此刻(死亡瞬间)按身份算好:特效延迟一帧执行时死者已被移出战斗,
					// CombatState 为 null、按小怪兜底,导致精英/BOSS 也只掉 1-2 缕。
					HextechCombatVfx.SoulDrain(dead, collector, HextechCombatVfx.GetSoulWispCount(dead));
				}
			}
		}
		catch (Exception ex)
		{
			HextechLog.Warn("CombatVfx", $"Soul drain dispatch on death anim failed: {ex.Message}");
		}
	}
}

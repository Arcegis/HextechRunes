using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

// PersonalHivePower 原版只为敌方养蜂人设计:受击时会把晕眩加入攻击者玩家的牌堆。
// 薄暮法衣旧版本可能已把它写进进行中的玩家战斗状态;这里不在 Hook 枚举期间移除能力,
// 只让非法的非敌方实例安全完成回调,以便原版伤害响应链正常 PopModel/收尾。
// 敌方“升级：蜂群术士”让所有敌人都带蜂房后,还要防住攻击者不是玩家也不是奥斯提的情况:
// 原版按 dealer.Player 生成晕眩,怪物攻击怪物时 Player 为空,这类伤害不给晕眩。
internal static class HextechPersonalHiveSafetyHooks
{
	/// <summary>原版的晕眩归属：奥斯提归主人，其余按攻击者自己的玩家；没有攻击者时原版自行跳过。</summary>
	internal static bool HasDazedRecipient(Creature? dealer)
	{
		if (dealer == null)
		{
			return true;
		}

		return dealer.Monster is Osty
			? dealer.PetOwner != null
			: dealer.Player != null;
	}

	[HarmonyPatch(
		typeof(PersonalHivePower),
		nameof(PersonalHivePower.AfterDamageReceived),
		new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel) })]
	[HextechPatch("compat.personal-hive", "私人蜂巢伤害响应安全")]
	private static class DamageResponsePatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(PersonalHivePower __instance, Creature? dealer, ref Task __result)
		{
			if (__instance.Owner?.Side == CombatSide.Enemy && HasDazedRecipient(dealer))
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}

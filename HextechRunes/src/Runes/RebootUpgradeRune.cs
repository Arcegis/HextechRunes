namespace HextechRunes;

/// <summary>
/// 升级：重启——重启的抽牌改为从抽牌堆选择对应数量的牌加入手牌。仍先把手牌洗回抽牌堆，选到的牌按抽牌结算。
/// </summary>
public sealed class RebootUpgradeRune : CardUpgradeRuneBase<Reboot>
{
	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);

	internal static async Task PlayUpgraded(PlayerChoiceContext choiceContext, Reboot card)
	{
		Player owner = card.Owner;
		await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);
		foreach (CardModel handCard in PileType.Hand.GetPile(owner).Cards.ToList())
		{
			await CardPileCmd.Add(handCard, PileType.Draw);
		}

		await CardPileCmd.Shuffle(choiceContext, owner);
		await HextechSelectedDrawHelper.DrawSelectedFromDrawPile(
			choiceContext,
			owner,
			(int)card.DynamicVars.Cards.BaseValue,
			fromHandDraw: false);
	}

	// 跳过型前缀：原版重启末尾直接 CardPileCmd.Draw 随机抽牌，没有"改为选牌"的 Hook。
	// 只对持有者照原版顺序重放动画、洗回与洗牌，最后一步换成选择抽牌。
	[HarmonyPatch(typeof(Reboot), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.reboot.select-draw", "升级重启", Rune = typeof(RebootUpgradeRune))]
	private static class RebootSelectDrawPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(Reboot __instance, PlayerChoiceContext choiceContext, ref Task __result)
		{
			if (__instance.Owner?.GetRelic<RebootUpgradeRune>() == null)
			{
				return true;
			}

			__result = PlayUpgraded(choiceContext, __instance);
			return false;
		}
	}
}

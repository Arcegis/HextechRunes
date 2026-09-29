namespace HextechRunes;

public sealed class RageUpgradeRune : CardUpgradeRuneBase<Rage>
{
	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Rage>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	// 效果常驻后每张狂怒都会永久叠层，给牌加上消耗词条作为刹车。
	public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
	{
		return Owner != null && card.Owner == Owner && card is Rage && keywords.Add(CardKeyword.Exhaust);
	}

	// 跳过理由：原版 RagePower.AfterSideTurnEnd(三个版本一致)在持有者回合结束时直接 PowerCmd.Remove(this)；
	// PowerCmd.Remove 不经过任何 Hook，没有办法让狂怒跨回合保留。激活条件：仅狂怒持有者拥有本符文时跳过，
	// 其余玩家走原版移除。原方法 IL 由 vanilla_copy_guard 冻结。
	[HarmonyPatch(typeof(RagePower), nameof(RagePower.AfterSideTurnEnd))]
	[HextechPatch("rune.rage.persistent", "升级狂怒", Rune = typeof(RageUpgradeRune))]
	private static class PersistentRagePatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		internal static bool Prefix(RagePower __instance, ref Task __result)
		{
			if (__instance.Owner.Player?.GetRelic<RageUpgradeRune>() == null)
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}

namespace HextechRunes;

public sealed class OblivionUpgradeRune : CardUpgradeRuneBase<Oblivion>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. base.ExtraHoverTips,
		HoverTipFactory.FromPower<OblivionPower>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);

	// 跳过理由：原版 OblivionPower.AfterSideTurnEnd(三个版本一致)在玩家侧回合结束时直接 PowerCmd.Remove(this)，
	// Remove 不经过任何 Hook，无法让敌人身上的遗忘跨回合保留。激活条件：仅当该遗忘的施加者(Applier)拥有本符文时跳过，
	// 其他玩家施加的遗忘仍按原版移除。原方法 IL 由 vanilla_copy_guard 冻结。
	[HarmonyPatch(typeof(OblivionPower), nameof(OblivionPower.AfterSideTurnEnd), typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IEnumerable<Creature>))]
	[HextechPatch("rune.oblivion", "升级遗忘", Rune = typeof(OblivionUpgradeRune))]
	private static class OblivionPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(OblivionPower __instance, ref Task __result)
		{
			if (__instance.Applier?.Player?.GetRelic<OblivionUpgradeRune>() == null)
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}

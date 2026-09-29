using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechForgeStackingHooks
{
	private static async Task<RelicModel> ObtainStackedForge(HextechForgeBase ownedForge)
	{
		ownedForge.AddForgeStack(flash: !ownedForge.HasUponPickupEffect);
		await ownedForge.AfterObtained();
		return ownedForge;
	}

	private static bool TryGetOwnedForge(Player player, RelicModel relic, [NotNullWhen(true)] out HextechForgeBase? ownedForge)
	{
		ModelId id = relic.CanonicalId();
		ownedForge = player.Relics
			.OfType<HextechForgeBase>()
			.FirstOrDefault(owned => owned.CanonicalId() == id);
		return ownedForge != null;
	}

	// 跳过型前缀（已裁决保留，见 architecture.md）：再次获得已持有的同名锻造器时改为给已有实例叠层。
	// 原版 RelicCmd.Obtain 没有"改为合并到已有遗物"的 Hook，而锻造器会经奖励、事件、商店多条路径直接 Obtain；
	// 只对本模组锻造器且玩家已持有同名实例时替换，替换体保留原版的遗物选择历史与"已见"记录。目标 IL 由原版拷贝守卫冻结。
	[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), typeof(RelicModel), typeof(Player), typeof(int))]
	[HextechPatch("forge.stacking", "锻造器叠层")]
	private static class ObtainPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(RelicModel relic, Player player, ref Task<RelicModel> __result)
		{
			if (relic is HextechForgeBase
				&& TryGetOwnedForge(player, relic, out HextechForgeBase? ownedForge)
				&& !ReferenceEquals(ownedForge, relic))
			{
				player.RunState.CurrentMapPointHistoryEntry?
					.GetEntry(player.NetId)
					.RelicChoices
					.Add(new ModelChoiceHistoryEntry(relic.Id, wasPicked: true));
				SaveManager.Instance.MarkRelicAsSeen(relic);
				__result = ObtainStackedForge(ownedForge);
				return false;
			}

			return true;
		}
	}
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechForgeStackingHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(RelicCmd), nameof(RelicCmd.Obtain), BindingFlags.Public | BindingFlags.Static, typeof(RelicModel), typeof(Player), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechForgeStackingHooks), nameof(ObtainPrefix)) { priority = Priority.Low });
	}

	private static bool ObtainPrefix(RelicModel relic, Player player, ref Task<RelicModel> __result)
	{
		if (relic is HextechForgeBase
			&& TryGetOwnedForge(player, relic, out HextechForgeBase? ownedForge)
			&& ownedForge != null
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

	private static async Task<RelicModel> ObtainStackedForge(HextechForgeBase ownedForge)
	{
		ownedForge.AddForgeStack(flash: !ownedForge.HasUponPickupEffect);
		await ownedForge.AfterObtained();
		return ownedForge;
	}

	private static bool TryGetOwnedForge(Player player, RelicModel relic, out HextechForgeBase? ownedForge)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		ownedForge = player.Relics
			.OfType<HextechForgeBase>()
			.FirstOrDefault(owned => (owned.CanonicalInstance?.Id ?? owned.Id) == id);
		return ownedForge != null;
	}

}

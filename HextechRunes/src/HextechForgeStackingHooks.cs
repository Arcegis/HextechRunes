using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MonoMod.RuntimeDetour;

namespace HextechRunes;

internal static class HextechForgeStackingHooks
{
	private static Hook? _obtainHook;

	private delegate Task<RelicModel> OrigObtain(RelicModel relic, Player player, int index);

	public static void Install()
	{
		_obtainHook = new Hook(
			RequireMethod(typeof(RelicCmd), nameof(RelicCmd.Obtain), BindingFlags.Public | BindingFlags.Static, typeof(RelicModel), typeof(Player), typeof(int)),
			ObtainDetour);
	}

	private static async Task<RelicModel> ObtainDetour(OrigObtain orig, RelicModel relic, Player player, int index)
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
			ownedForge.AddForgeStack(flash: !ownedForge.HasUponPickupEffect);
			await ownedForge.AfterObtained();
			return ownedForge;
		}

		return await orig(relic, player, index);
	}

	private static bool TryGetOwnedForge(Player player, RelicModel relic, out HextechForgeBase? ownedForge)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		ownedForge = player.Relics
			.OfType<HextechForgeBase>()
			.FirstOrDefault(owned => (owned.CanonicalInstance?.Id ?? owned.Id) == id);
		return ownedForge != null;
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}

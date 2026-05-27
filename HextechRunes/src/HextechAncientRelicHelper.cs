using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechAncientRelicHelper
{
	private static readonly Type[] NonupeipeRelicTypes =
	[
		typeof(BlessedAntler),
		typeof(BrilliantScarf),
		typeof(DelicateFrond),
		typeof(DiamondDiadem),
		typeof(FurCoat),
		typeof(Glitter),
		typeof(JewelryBox),
		typeof(LoomingFruit),
		typeof(SignetRing),
		typeof(BeautifulBracelet)
	];

	public static RelicModel CreateRandomNonupeipeRelic(Player player, string source)
	{
		Type[] candidates = NonupeipeRelicTypes
			.Where(type => !HasRelic(player, type))
			.ToArray();
		if (candidates.Length == 0)
		{
			candidates = NonupeipeRelicTypes;
		}

		Type relicType = HextechStableRandom.Pick(
			candidates,
			(RunState)player.RunState,
			HextechStableRandom.TypeModelKey,
			source,
			HextechStableRandom.PlayerKey(player),
			player.Relics.Count.ToString());
		return ModelDb.GetById<RelicModel>(ModelDb.GetId(relicType)).ToMutable();
	}

	public static RelicModel CreateWaxRelicFromRewardPool(Player player)
	{
		RelicModel relic = RelicFactory.PullNextRelicFromFront(player);
		relic.IsWax = true;
		return relic;
	}

	private static bool HasRelic(Player player, Type relicType)
	{
		ModelId relicId = ModelDb.GetId(relicType);
		return player.Relics.Any(relic => (relic.CanonicalInstance?.Id ?? relic.Id) == relicId);
	}
}

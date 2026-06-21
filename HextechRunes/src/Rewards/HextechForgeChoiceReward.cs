using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

internal sealed class HextechForgeChoiceReward : Reward
{
	private readonly List<RelicModel> _options;

	public HextechForgeChoiceReward(IReadOnlyList<RelicModel> options, Player player)
		: base(player)
	{
		_options = options.Select(CreateMutableOption).ToList();
	}

	protected override RewardType RewardType => RewardType.Relic;

	public override int RewardsSetIndex => 4;

	public override LocString Description => new("relic_collection", "HEXTECH_FORGE_CHOICE_REWARD");

	public override bool IsPopulated => _options.Count > 0;

	protected override string IconPath => GetForgeRewardIconPath();

	internal ModelId ClaimedForgeId { get; private set; } = ModelId.none;

#if STS2_105_OR_NEWER
	public override void Populate()
	{
		MarkContentAsSeen();
	}
#else
	public override Task Populate()
	{
		MarkContentAsSeen();
		return Task.CompletedTask;
	}
#endif

	protected override async Task<bool> OnSelect()
	{
		// RewardSynchronizer already broadcasts the obtained forge. Reserving a
		// PlayerChoiceSynchronizer id here is unsafe because reward OnSelect runs
		// only on the choosing client, which desyncs vanilla choice counters.
		RelicModel? selected = await HextechForgeSelectionCoordinator.SelectForge(Player, _options, "reward", syncMultiplayerChoice: false);
		if (selected == null)
		{
			return false;
		}

		ClaimedForgeId = selected.CanonicalInstance?.Id ?? selected.Id;
		await HextechForgeGrantHelper.ObtainSelectedForge(Player, selected, syncObtainedRelic: true);
		Log.Info($"[{ModInfo.Id}][ForgeChoiceReward] Obtained selected forge: player={Player.NetId} relic={(selected.CanonicalInstance?.Id ?? selected.Id).Entry}");
		return true;
	}

	public override SerializableReward ToSerializable()
	{
		return new SerializableReward
		{
			// Gold rewards deserialize safely before our postfix replaces the marker with this custom reward.
			RewardType = RewardType.Gold,
			GoldAmount = 0,
			CardPoolIds = _options.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id).ToList(),
			OptionCount = _options.Count,
			CustomDescriptionEncounterSourceId = ModelDb.GetId<RandomForgeShopRelic>(),
		};
	}

	public override void MarkContentAsSeen()
	{
		foreach (RelicModel relic in _options)
		{
			SaveManager.Instance.MarkRelicAsSeen(relic);
		}
	}

	internal static HextechForgeChoiceReward FromSavedReward(SerializableReward save, Player player)
	{
		List<RelicModel> options = save.CardPoolIds
			.Select(id => ModelDb.GetById<RelicModel>(id).ToMutable())
			.Where(HextechCatalog.IsHextechForgeRelic)
			.Take(Math.Max(0, save.OptionCount))
			.ToList();
		return new HextechForgeChoiceReward(options, player);
	}

	private static RelicModel CreateMutableOption(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelDb.GetById<RelicModel>(id).ToMutable();
	}

	private string GetForgeRewardIconPath()
	{
		RelicModel? firstOption = _options.FirstOrDefault();
		return firstOption != null && HextechCatalog.TryGetForgeRarity(firstOption, out HextechRarityTier rarity)
			? HextechAssets.GetForgeIconPath(rarity)
			: ImageHelper.GetImagePath("ui/reward_screen/reward_icon_relic.png");
	}
}

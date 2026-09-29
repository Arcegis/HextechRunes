using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace HextechRunes;

public sealed class CrossOrbRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CommonReductionPercent", 50m)
	];

	// 先确定奖励牌，再由 Late 阶段的华美发束等遗物附魔，避免换牌丢掉一次性奖励效果。
	public override bool TryModifyCardRewardOptions(
		Player player,
		List<CardCreationResult> cardRewardOptions,
		CardCreationOptions creationOptions)
	{
		if (player != Owner || cardRewardOptions.Count == 0)
		{
			return false;
		}

		return ReplaceCommonCards(player, cardRewardOptions, creationOptions, "card-reward");
	}

	public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
	{
		if (player != Owner || cards.Count == 0)
		{
			return;
		}

		CardCreationOptions creationOptions = HextechGameApiCompat.CreateOptionsFromCards(
			player,
			player.Character.CardPool.AllCards
				.Concat(ModelDb.CardPool<ColorlessCardPool>().AllCards)
				.Where(static card => card.Rarity != CardRarity.Common && card.CanBeGeneratedByModifiers)
				.ToList(),
			CardCreationSource.Shop,
			CardRarityOddsType.Uniform);
		ReplaceCommonCards(player, cards, creationOptions, "merchant-card");
	}

	private bool ReplaceCommonCards(
		Player player,
		List<CardCreationResult> results,
		CardCreationOptions creationOptions,
		string saltScope)
	{
		bool modified = false;
		for (int i = 0; i < results.Count; i++)
		{
			CardCreationResult result = results[i];
			if (result.Card.Rarity != CardRarity.Common
				|| !ShouldReplaceCommon(player, saltScope, i.ToString(), result.Card.Id.Entry)
				|| !TryCreateNonCommonCard(player, result.Card, creationOptions, results, out CardCreationResult? replacement))
			{
				continue;
			}

			result.ModifyCard(replacement.Card, this);
			modified = true;
		}

		if (modified)
		{
			Flash();
		}

		return modified;
	}

	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (player != Owner || rewards.Count == 0)
		{
			return false;
		}

		bool modified = false;
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] is PotionReward potionReward
				&& potionReward.Potion?.Rarity == PotionRarity.Common
				&& ShouldReplaceCommon(player, "potion-reward", i.ToString(), potionReward.Potion.Id.Entry)
				&& TryCreateNonCommonPotionReward(player, i, out PotionReward? potionReplacement))
			{
				rewards[i] = potionReplacement;
				modified = true;
				continue;
			}

			if (rewards[i] is RelicReward relicReward
				&& relicReward.Rarity == RelicRarity.Common
				&& ShouldReplaceCommon(player, "relic-reward", i.ToString(), room?.RoomType.ToString() ?? "none"))
			{
				rewards[i] = new RelicReward(PickNonCommonRelicRarity(player, i, room), player);
				modified = true;
			}
		}

		if (modified)
		{
			Flash();
		}

		return modified;
	}

	private bool ShouldReplaceCommon(Player player, params string?[] saltParts)
	{
		return HextechStableRandom.PercentChance(
			(RunState)player.RunState,
			DynamicVars["CommonReductionPercent"].IntValue,
			["cross-orb", HextechStableRandom.PlayerKey(player), .. saltParts]);
	}

	private static bool TryCreateNonCommonCard(
		Player player,
		CardModel sourceCard,
		CardCreationOptions creationOptions,
		IEnumerable<CardCreationResult> currentResults,
		[NotNullWhen(true)] out CardCreationResult? result)
	{
		if (!TryGetCardPoolId(sourceCard, out ModelId sourcePoolId))
		{
			result = null;
			return false;
		}

		HashSet<ModelId> existingIds = currentResults
			.Select(static option => option.Card.CanonicalInstance.Id)
			.ToHashSet();
		List<CardModel> candidates = creationOptions
			.GetPossibleCards(player)
			.Where(card => card.Rarity != CardRarity.Common
				&& !existingIds.Contains(card.Id)
				&& TryGetCardPoolId(card, out ModelId candidatePoolId)
				&& candidatePoolId.Equals(sourcePoolId))
			.ToList();
		if (candidates.Count == 0)
		{
			result = null;
			return false;
		}

		CardCreationOptions nonCommonOptions = HextechGameApiCompat.CreateOptionsFromCards(
				player,
				candidates,
				creationOptions.Source,
				CardRarityOddsType.Uniform)
			.WithFlags(creationOptions.Flags | CardCreationFlags.NoModifyHooks);
		result = CardFactory.CreateForReward(player, 1, nonCommonOptions).FirstOrDefault();
		if (result != null)
		{
			CardTransformUpgradeHelper.PreserveUpgradeLevel(sourceCard, result.Card);
		}

		return result != null;
	}

	private static bool TryGetCardPoolId(CardModel card, out ModelId id)
	{
		try
		{
			id = card.Pool.Id;
			return true;
		}
		catch (InvalidProgramException)
		{
			// 原版 CardModel.Pool(0.111.0 反编译)在卡不属于任何卡池时抛 InvalidProgramException；
			// 第三方或事件生成的这类卡不参与同池替换。
			id = ModelId.none;
			return false;
		}
	}

	private static bool TryCreateNonCommonPotionReward(Player player, int rewardIndex, [NotNullWhen(true)] out PotionReward? reward)
	{
		List<PotionModel> candidates = HextechGameApiCompat.GetPotionOptions(player)
			.Where(static potion => potion.Rarity != PotionRarity.Common)
			.ToList();
		if (candidates.Count == 0)
		{
			reward = null;
			return false;
		}

		PotionModel potion = HextechStableRandom.Pick(
			candidates,
			(RunState)player.RunState,
			HextechStableRandom.PotionKey,
			"cross-orb-non-common-potion",
			HextechStableRandom.PlayerKey(player),
			rewardIndex.ToString()).ToMutable();
		reward = new PotionReward(potion, player);
		return true;
	}

	private static RelicRarity PickNonCommonRelicRarity(Player player, int rewardIndex, AbstractRoom? room)
	{
		const int rarePercentAmongNonCommonRelics = 34;
		return HextechStableRandom.PercentChance(
			(RunState)player.RunState,
			rarePercentAmongNonCommonRelics,
			[
				"cross-orb-non-common-relic-rarity",
				HextechStableRandom.PlayerKey(player),
				rewardIndex.ToString(),
				room?.RoomType.ToString() ?? "none"
			])
			? RelicRarity.Rare
			: RelicRarity.Uncommon;
	}
}

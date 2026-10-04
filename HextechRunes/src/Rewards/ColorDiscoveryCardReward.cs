using System.Diagnostics.CodeAnalysis;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal sealed class ColorDiscoveryCardReward : CardReward
{
	private static readonly FieldInfo? SpecialCardRewardCardField = TryGetField(typeof(SpecialCardReward), "_card");

	private readonly ModelId _cardId;
	private readonly CardCreationSource _source;
	private readonly CardRarityOddsType _rarityOdds;

	public ColorDiscoveryCardReward(
		ModelId cardId,
		Player player,
		CardCreationSource source = CardCreationSource.Encounter,
		CardRarityOddsType rarityOdds = CardRarityOddsType.Uniform)
		: base(CreateCardsToOffer(cardId, player), source, player, CreateRerollOptions(cardId, source, rarityOdds))
	{
		_cardId = cardId;
		_source = source;
		_rarityOdds = rarityOdds;
		CanReroll = false;
	}

	private ColorDiscoveryCardReward(
		CardModel card,
		ModelId cardId,
		Player player,
		CardCreationSource source,
		CardRarityOddsType rarityOdds)
		: base([card], source, player, CreateRerollOptions(cardId, source, rarityOdds))
	{
		_cardId = cardId;
		_source = source;
		_rarityOdds = rarityOdds;
		CanReroll = false;
	}

	public static ColorDiscoveryCardReward FromSavedReward(SerializableReward save, Player player)
	{
		CardCreationSource source = save.Source;
		CardRarityOddsType rarityOdds = save.RarityOdds;
		return new ColorDiscoveryCardReward(save.PredeterminedModelId, player, source, rarityOdds);
	}

	internal static bool TryFromSavedSpecialCardReward(
		SerializableReward save,
		Reward? restoredReward,
		Player player,
		[NotNullWhen(true)] out ColorDiscoveryCardReward? reward,
		bool logFailure = true)
	{
		reward = null;
		CardModel? card = TryGetRestoredSpecialCard(restoredReward, SpecialCardRewardCardField);
		if (card == null)
		{
			if (logFailure
				&& restoredReward != null
				&& HextechRunLogBudget.TryConsume("rewards.color-discovery-special-card-restore", 1))
			{
				HextechLog.Warn(
					"Rewards", $"Color Discovery reward kept as the original SpecialCardReward because its restored card could not be read; "
					+ $"rewardType={restoredReward.GetType().FullName} fieldAvailable={SpecialCardRewardCardField != null}.");
			}

			return false;
		}

		ModelId cardId = card.CanonicalId();
		reward = new ColorDiscoveryCardReward(card, cardId, player, save.Source, save.RarityOdds);
		return true;
	}

	public override SerializableReward ToSerializable()
	{
		CardModel card = Cards.FirstOrDefault() ?? ModelDb.GetById<CardModel>(_cardId);
		return new SerializableReward
		{
			RewardType = RewardType.SpecialCard,
			Source = _source,
			RarityOdds = _rarityOdds,
			OptionCount = 1,
			SpecialCard = card.ToSerializable(),
			PredeterminedModelId = ModelDb.GetId<ColorDiscoveryRune>(),
		};
	}

	private static IEnumerable<CardModel> CreateCardsToOffer(ModelId cardId, Player player)
	{
		CardModel canonicalCard = ModelDb.GetById<CardModel>(cardId);
		yield return player.RunState.CreateCard(canonicalCard, player);
	}

	private static CardCreationOptions CreateRerollOptions(
		ModelId cardId,
		CardCreationSource source,
		CardRarityOddsType rarityOdds)
	{
		CardModel canonicalCard = ModelDb.GetById<CardModel>(cardId);
		CardCreationOptions options = HextechGameApiCompat.CreateOptionsForSingleCard(canonicalCard, source, rarityOdds);
		options.WithFlags(CardCreationFlags.IsCardReward);
		return options;
	}

	// 其他模组的后缀可能已把恢复结果换成别的奖励类型，只从 SpecialCardReward 上读字段。
	internal static CardModel? TryGetRestoredSpecialCard(object? restoredReward, FieldInfo? cardField)
	{
		return restoredReward is SpecialCardReward && cardField != null
			? cardField.GetValue(restoredReward) as CardModel
			: null;
	}
}

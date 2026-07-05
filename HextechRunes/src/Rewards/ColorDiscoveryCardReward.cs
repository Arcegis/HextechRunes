using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal sealed class ColorDiscoveryCardReward : CardReward
{
	private static readonly FieldInfo CardRewardCardsField = RequireField(typeof(CardReward), "_cards");
	private static readonly FieldInfo SpecialCardRewardCardField = RequireField(typeof(SpecialCardReward), "_card");

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

	public static ColorDiscoveryCardReward FromSavedSpecialCardReward(SerializableReward save, Reward restoredReward, Player player)
	{
		if (save.SpecialCard == null)
		{
			throw new InvalidOperationException("Color Discovery card reward is missing its serialized card.");
		}

		CardModel? card = SpecialCardRewardCardField.GetValue(restoredReward) as CardModel;
		if (card == null)
		{
			card = CardModel.FromSerializable(save.SpecialCard);
			player.RunState.AddCard(card, player);
		}

		ModelId cardId = card.CanonicalInstance?.Id ?? card.Id;
		return new ColorDiscoveryCardReward(card, cardId, player, save.Source, save.RarityOdds);
	}

	public override SerializableReward ToSerializable()
	{
		CardModel card = GetCurrentRewardCard() ?? ModelDb.GetById<CardModel>(_cardId);
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
#if STS2_108_OR_NEWER
		// 0.108.0 无自定义卡列表构造:用该卡所属池+按 Id 过滤等价表达"只出这张卡"。
		CardCreationOptions options = new([canonicalCard.Pool], source, rarityOdds, card => card.Id.Equals(cardId));
#else
		CardCreationOptions options = new([canonicalCard], source, rarityOdds);
#endif
#if STS2_105_OR_NEWER
		options.WithFlags(CardCreationFlags.IsCardReward);
#endif
		return options;
	}

	private CardModel? GetCurrentRewardCard()
	{
		if (CardRewardCardsField.GetValue(this) is not IEnumerable<CardCreationResult> cards)
		{
			return null;
		}

		return cards.FirstOrDefault()?.Card;
	}
}

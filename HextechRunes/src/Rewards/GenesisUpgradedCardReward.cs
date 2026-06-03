using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Assets;
using Godot;

namespace HextechRunes;

internal sealed class GenesisUpgradedCardReward : Reward
{
	private static readonly IReadOnlyList<CardRewardAlternative> SkipOnlyAlternatives = new[]
	{
#if STS2_105_OR_NEWER
		new CardRewardAlternative("Skip", PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward),
#else
		new CardRewardAlternative("Skip", PostAlternateCardRewardAction.DismissScreenAndKeepReward),
#endif
	};

	private readonly List<CardCreationResult> _cards = new();
	private NCardRewardSelectionScreen? _currentlyShownScreen;

	public GenesisUpgradedCardReward(CardCreationOptions options, int cardCount, Player player)
		: base(player)
	{
		Options = options;
		OptionCount = cardCount;
	}

	protected override RewardType RewardType => RewardType.Card;

	public override int RewardsSetIndex => 5;

	public override LocString Description => new("gameplay_ui", "COMBAT_REWARD_ADD_CARD");

	public override bool IsPopulated => _cards.Count > 0;

	protected override string IconPath => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_card.png");

	private int OptionCount { get; }

	private CardCreationOptions Options { get; }

	private void GenerateCards()
	{
		if (_cards.Count > 0)
		{
			return;
		}

		_cards.AddRange(CardFactory.CreateForReward(Player, OptionCount, Options));
		foreach (CardModel card in _cards.Select(result => result.Card))
		{
			if (card.IsUpgradable && !card.IsUpgraded)
			{
				CardCmd.Upgrade(card, CardPreviewStyle.None);
			}
		}
	}

#if STS2_105_OR_NEWER
	public override void Populate()
	{
		GenerateCards();
	}
#else
	public override Task Populate()
	{
		GenerateCards();
		return Task.CompletedTask;
	}
#endif

	protected override async Task<bool> OnSelect()
	{
		Log.Info("Genesis upgraded card reward selected");
		_currentlyShownScreen = NCardRewardSelectionScreen.ShowScreen(_cards, SkipOnlyAlternatives);

#if STS2_105_OR_NEWER
		int? selectedIndex = _currentlyShownScreen != null
			? await _currentlyShownScreen.OptionSelected()
			: null;
		bool removeReward = selectedIndex.HasValue;
		CardModel? result = selectedIndex is >= 0 && selectedIndex.Value < _cards.Count
			? _cards[selectedIndex.Value].Card
			: null;
		NCardHolder? cardHolder = null;
#else
		Tuple<IEnumerable<NCardHolder>, bool> selected = _currentlyShownScreen != null
			? await _currentlyShownScreen.CardsSelected()
			: new Tuple<IEnumerable<NCardHolder>, bool>(Enumerable.Empty<NCardHolder>(), false);

		bool removeReward = selected.Item2;
		NCardHolder? cardHolder = selected.Item1.FirstOrDefault();
		CardModel? result = cardHolder?.CardNode?.Model;
#endif
		if (result != null)
		{
			CardPileAddResult added = await CardPileCmd.Add(result, PileType.Deck);
			if (added.success)
			{
				result = added.cardAdded;
				_cards.RemoveAll(card => card.Card == result);
				PlayCardRewardFlyVfx(cardHolder, result);
				Log.Info($"Obtained {result.Id} from Genesis upgraded card reward");
				TrySyncObtainedCard(result);
			}
		}

		RemoveSelectionScreen();
		return removeReward;
	}

	public override void OnSkipped()
	{
		RemoveSelectionScreen();
	}

	public override SerializableReward ToSerializable()
	{
		return new SerializableReward
		{
			RewardType = RewardType.Card,
			Source = Options.Source,
			RarityOdds = Options.RarityOdds,
			CardPoolIds = Options.CardPools.Select(pool => pool.Id).ToList(),
			OptionCount = OptionCount,
			CustomDescriptionEncounterSourceId = ModelDb.GetId<GenesisRune>(),
		};
	}

	public override void MarkContentAsSeen()
	{
	}

	private static void PlayCardRewardFlyVfx(NCardHolder? cardHolder, CardModel result)
	{
		if (cardHolder?.CardNode is not { } cardNode || NRun.Instance?.GlobalUi == null)
		{
			return;
		}

		NRun.Instance.GlobalUi.ReparentCard(cardNode);
		cardHolder.QueueFreeSafely();
		Vector2 targetPosition = PileType.Deck.GetTargetPosition(cardNode);
#if STS2_105_OR_NEWER
		NRun.Instance.GlobalUi.TopBar.TrailContainer.AddChildSafely(
			NCardFlyVfx.Create(cardNode, PileType.Deck, isAddingToPile: true, result.Owner.Character.TrailPath));
#else
		NRun.Instance.GlobalUi.TopBar.TrailContainer.AddChildSafely(
			NCardFlyVfx.Create(cardNode, targetPosition, isAddingToPile: true, result.Owner.Character.TrailPath));
#endif
	}

	private static void TrySyncObtainedCard(CardModel result)
	{
		try
		{
			RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedCard(result);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Rewards] Failed to sync Genesis reward card {result.Id}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private void RemoveSelectionScreen()
	{
		if (_currentlyShownScreen == null)
		{
			return;
		}

		NOverlayStack.Instance?.Remove(_currentlyShownScreen);
		_currentlyShownScreen = null;
	}
}

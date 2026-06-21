using System.Reflection;
using System.Threading;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class DoubleVisionRune : HextechRelicBase
{
	private static readonly AsyncLocal<CardRewardTracker?> CurrentCardRewardTracker = new();
	private static readonly FieldInfo? GoldRewardWasStolenBackField = typeof(GoldReward).GetField("_wasGoldStolenBack", BindingFlags.Instance | BindingFlags.NonPublic);

	public override async Task AfterRewardTaken(Player player, Reward reward)
	{
		if (Owner == null || !ReferenceEquals(player, Owner) || player.Creature.IsDead)
		{
			return;
		}

		switch (reward)
		{
			case GoldReward goldReward:
				await DuplicateGoldReward(player, goldReward);
				break;
			case PotionReward potionReward:
				await DuplicatePotionReward(player, potionReward);
				break;
			case HextechForgeChoiceReward forgeReward:
				await DuplicateForgeReward(player, forgeReward);
				break;
		}
	}

	internal static object? BeginCardRewardTracking(Player player)
	{
		IReadOnlyList<DoubleVisionRune> runes = GetActiveRunes(player);
		if (runes.Count == 0)
		{
			return null;
		}

		CardRewardTracker? previousTracker = CurrentCardRewardTracker.Value;
		CardRewardTrackingScope scope = new(player, runes, previousTracker);
		CurrentCardRewardTracker.Value = scope.Tracker;
		return scope;
	}

	internal static Task<bool> CompleteCardRewardAsync(Task<bool> originalTask, object? trackingState)
	{
		if (trackingState is not CardRewardTrackingScope scope)
		{
			return originalTask;
		}

		CurrentCardRewardTracker.Value = scope.PreviousTracker;
		return CompleteCardRewardAddTrackingAsync(originalTask, scope);
	}

	internal static object? CaptureRewardDuplicationState(Player player)
	{
		IReadOnlyList<DoubleVisionRune> runes = GetActiveRunes(player);
		return runes.Count == 0 ? null : new RewardDuplicationScope(runes);
	}

	internal static Task<bool> CompleteRelicRewardAsync(RelicReward reward, Task<bool> originalTask, object? duplicationState)
	{
		if (duplicationState is not RewardDuplicationScope scope)
		{
			return originalTask;
		}

		return CompleteRelicRewardAsync(reward, originalTask, scope);
	}

	internal static void TrackCardPileAdd(CardModel card, PileType newPileType, AbstractModel? clonedBy, ref Task<CardPileAddResult> resultTask)
	{
		CardRewardTracker? tracker = CurrentCardRewardTracker.Value;
		if (tracker == null
			|| newPileType != PileType.Deck
			|| clonedBy is DoubleVisionRune
			|| card.Owner != tracker.Player)
		{
			return;
		}

		resultTask = TrackCardPileAddAsync(resultTask, tracker);
	}

	private static async Task<bool> CompleteCardRewardAddTrackingAsync(Task<bool> originalTask, CardRewardTrackingScope scope)
	{
		bool rewardComplete = await originalTask;

		if (!rewardComplete || scope.Tracker.AddedCards.Count == 0)
		{
			return rewardComplete;
		}

		foreach (DoubleVisionRune rune in scope.Runes)
		{
			await rune.DuplicateRewardCards(scope.Tracker.AddedCards);
		}

		return rewardComplete;
	}

	private static async Task<bool> CompleteRelicRewardAsync(RelicReward reward, Task<bool> originalTask, RewardDuplicationScope scope)
	{
		bool rewardComplete = await originalTask;
		if (!rewardComplete || reward.ClaimedRelic == null)
		{
			return rewardComplete;
		}

		foreach (DoubleVisionRune rune in scope.Runes)
		{
			await rune.DuplicateRelicReward(reward.Player, reward);
		}

		return rewardComplete;
	}

	private static async Task<CardPileAddResult> TrackCardPileAddAsync(Task<CardPileAddResult> originalTask, CardRewardTracker tracker)
	{
		CardPileAddResult result = await originalTask;
		if (result.success
			&& result.cardAdded.Owner == tracker.Player
			&& result.cardAdded.Pile?.Type == PileType.Deck)
		{
			tracker.AddedCards.Add(result.cardAdded);
		}

		return result;
	}

	private async Task DuplicateRewardCards(IReadOnlyList<CardModel> sourceCards)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<CardPileAddResult> results = new();
		foreach (CardModel sourceCard in sourceCards)
		{
			if (sourceCard.Owner != Owner || !Owner.RunState.ContainsCard(sourceCard))
			{
				continue;
			}

			CardModel copy = Owner.RunState.CloneCard(sourceCard);
			CardPileAddResult result = await CardPileCmd.Add(copy, PileType.Deck, clonedBy: this);
			if (!result.success)
			{
				continue;
			}

			SaveManager.Instance.MarkCardAsSeen(result.cardAdded);
			TrySyncObtainedCard(result.cardAdded);
			results.Add(result);
		}

		if (results.Count > 0)
		{
			Flash();
			CardCmd.PreviewCardPileAdd(results, 2f);
		}
	}

	private async Task DuplicateGoldReward(Player player, GoldReward reward)
	{
		if (reward.Amount <= 0)
		{
			return;
		}

		Flash();
		bool wasGoldStolenBack = GoldRewardWasStolenBackField?.GetValue(reward) is true;
		await PlayerCmd.GainGold(reward.Amount, player, wasGoldStolenBack);
		TrySyncObtainedGold(reward.Amount);
	}

	private async Task DuplicatePotionReward(Player player, PotionReward reward)
	{
		PotionModel? claimedPotion = reward.ClaimedPotion;
		if (claimedPotion == null)
		{
			return;
		}

		PotionModel copy = ModelDb.GetById<PotionModel>(claimedPotion.CanonicalInstance?.Id ?? claimedPotion.Id).ToMutable();
		PotionProcureResult result = await PotionCmd.TryToProcure(copy, player);
		if (!result.success)
		{
			return;
		}

		Flash();
		TrySyncObtainedPotion(result.potion);
	}

	private async Task DuplicateRelicReward(Player player, RelicReward reward)
	{
		RelicModel? claimedRelic = reward.ClaimedRelic;
		if (claimedRelic == null)
		{
			return;
		}

		RelicModel copy = ModelDb.GetById<RelicModel>(claimedRelic.CanonicalInstance?.Id ?? claimedRelic.Id).ToMutable();
		RelicModel obtained = await RelicCmd.Obtain(copy, player);
		Flash();
		TrySyncObtainedRelic(obtained);
	}

	private async Task DuplicateForgeReward(Player player, HextechForgeChoiceReward reward)
	{
		if (reward.ClaimedForgeId == ModelId.none)
		{
			return;
		}

		RelicModel forge = ModelDb.GetById<RelicModel>(reward.ClaimedForgeId).ToMutable();
		Flash();
		await HextechForgeGrantHelper.ObtainSelectedForge(player, forge, syncObtainedRelic: true);
	}

	private static IReadOnlyList<DoubleVisionRune> GetActiveRunes(Player player)
	{
		if (player.Creature.IsDead)
		{
			return [];
		}

		return player.Relics
			.OfType<DoubleVisionRune>()
			.Where(static rune => rune.Owner != null)
			.ToList();
	}

	private static bool ShouldSyncReward()
	{
		RunManager? runManager = RunManager.Instance;
		INetGameService? netService = runManager?.NetService;
		return netService != null
			&& netService.Type is NetGameType.Host or NetGameType.Client
			&& netService.IsConnected;
	}

	private static void TrySyncObtainedCard(CardModel card)
	{
		if (!ShouldSyncReward())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedCard(card);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][DoubleVision] Failed to sync duplicated card reward {card.Id.Entry}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedGold(int amount)
	{
		if (!ShouldSyncReward())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(amount);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][DoubleVision] Failed to sync duplicated gold reward {amount}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedPotion(PotionModel potion)
	{
		if (!ShouldSyncReward())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedPotion(potion);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][DoubleVision] Failed to sync duplicated potion reward {potion.Id.Entry}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedRelic(RelicModel relic)
	{
		if (!ShouldSyncReward())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][DoubleVision] Failed to sync duplicated relic reward {relic.Id.Entry}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private sealed class CardRewardTracker
	{
		public CardRewardTracker(Player player)
		{
			Player = player;
		}

		public Player Player { get; }

		public List<CardModel> AddedCards { get; } = [];
	}

	private class RewardDuplicationScope
	{
		public RewardDuplicationScope(IReadOnlyList<DoubleVisionRune> runes)
		{
			Runes = runes;
		}

		public IReadOnlyList<DoubleVisionRune> Runes { get; }
	}

	private sealed class CardRewardTrackingScope : RewardDuplicationScope
	{
		public CardRewardTrackingScope(Player player, IReadOnlyList<DoubleVisionRune> runes, CardRewardTracker? previousTracker)
			: base(runes)
		{
			Tracker = new CardRewardTracker(player);
			PreviousTracker = previousTracker;
		}

		public CardRewardTracker Tracker { get; }

		public CardRewardTracker? PreviousTracker { get; }
	}
}

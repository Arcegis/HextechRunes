using System.Reflection;
using System.Threading;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class DoubleVisionRune : HextechRelicBase
{
	private static readonly AsyncLocal<CardRewardTracker?> CurrentCardRewardTracker = new();
	private static readonly AsyncLocal<int> CommandDuplicationSuppressionDepth = new();
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

	internal static object? BeginRewardCommandSuppression()
	{
		int previousDepth = CommandDuplicationSuppressionDepth.Value;
		CommandDuplicationSuppressionDepth.Value = previousDepth + 1;
		return previousDepth;
	}

	internal static void CompleteRewardCommandSuppression(object? suppressionState)
	{
		if (suppressionState is int previousDepth)
		{
			CommandDuplicationSuppressionDepth.Value = previousDepth;
		}
	}

	internal static object? BeginDirectRelicReward(Player player)
	{
		return BeginDirectCommandReward(player);
	}

	internal static Task<RelicModel> CompleteDirectRelicRewardAsync(Task<RelicModel> originalTask, object? duplicationState)
	{
		if (duplicationState is not DirectCommandRewardScope scope)
		{
			return originalTask;
		}

		RestoreCommandRewardScope(scope);
		return CompleteDirectRelicRewardAsync(originalTask, scope);
	}

	internal static object? BeginDirectPotionReward(Player player)
	{
		return BeginDirectCommandReward(player);
	}

	internal static Task<PotionProcureResult> CompleteDirectPotionRewardAsync(Task<PotionProcureResult> originalTask, object? duplicationState)
	{
		if (duplicationState is not DirectCommandRewardScope scope)
		{
			return originalTask;
		}

		RestoreCommandRewardScope(scope);
		return CompleteDirectPotionRewardAsync(originalTask, scope);
	}

	internal static object? BeginDirectGoldReward(Player player, decimal amount, bool wasStolenBack)
	{
		DirectCommandRewardScope? scope = BeginDirectCommandReward(player);
		if (scope == null)
		{
			return null;
		}

		scope.GoldAmount = amount;
		scope.WasGoldStolenBack = wasStolenBack;
		return scope;
	}

	internal static Task CompleteDirectGoldRewardAsync(Task originalTask, object? duplicationState)
	{
		if (duplicationState is not DirectCommandRewardScope scope)
		{
			return originalTask;
		}

		RestoreCommandRewardScope(scope);
		return CompleteDirectGoldRewardAsync(originalTask, scope);
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
		if (tracker != null)
		{
			if (newPileType == PileType.Deck
				&& clonedBy is not DoubleVisionRune
				&& card.Owner == tracker.Player)
			{
				resultTask = TrackCardPileAddAsync(resultTask, tracker);
			}

			return;
		}

		if (!ShouldDuplicateDirectDeckCard(card, newPileType, clonedBy))
		{
			return;
		}

		resultTask = CompleteDirectDeckCardRewardAsync(resultTask);
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

	private static async Task<RelicModel> CompleteDirectRelicRewardAsync(Task<RelicModel> originalTask, DirectCommandRewardScope scope)
	{
		RelicModel obtainedRelic = await originalTask;
		if (obtainedRelic.Owner == null || obtainedRelic.Owner.Creature.IsDead)
		{
			return obtainedRelic;
		}

		foreach (DoubleVisionRune rune in scope.Runes)
		{
			await rune.DuplicateObtainedRelic(obtainedRelic.Owner, obtainedRelic);
		}

		return obtainedRelic;
	}

	private static async Task<PotionProcureResult> CompleteDirectPotionRewardAsync(Task<PotionProcureResult> originalTask, DirectCommandRewardScope scope)
	{
		PotionProcureResult result = await originalTask;
		if (!result.success || result.potion.Owner == null || result.potion.Owner.Creature.IsDead)
		{
			return result;
		}

		foreach (DoubleVisionRune rune in scope.Runes)
		{
			await rune.DuplicateObtainedPotion(result.potion.Owner, result.potion);
		}

		return result;
	}

	private static async Task CompleteDirectGoldRewardAsync(Task originalTask, DirectCommandRewardScope scope)
	{
		await originalTask;

		int amount = (int)scope.GoldAmount;
		if (amount <= 0 || scope.Player.Creature.IsDead)
		{
			return;
		}

		foreach (DoubleVisionRune rune in scope.Runes)
		{
			await rune.DuplicateGoldAmount(scope.Player, amount, scope.WasGoldStolenBack);
		}
	}

	private static async Task<CardPileAddResult> CompleteDirectDeckCardRewardAsync(Task<CardPileAddResult> originalTask)
	{
		CardPileAddResult result = await originalTask;
		if (!result.success
			|| result.cardAdded.Pile?.Type != PileType.Deck
			|| result.cardAdded.Owner == null
			|| !ShouldDuplicateForPlayer(result.cardAdded.Owner))
		{
			return result;
		}

		IReadOnlyList<DoubleVisionRune> runes = GetActiveRunes(result.cardAdded.Owner);
		foreach (DoubleVisionRune rune in runes)
		{
			await rune.DuplicateRewardCards(new[] { result.cardAdded });
		}

		return result;
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
			CardPileAddResult result = await RunWithCommandDuplicationSuppressed(
				() => CardPileCmd.Add(copy, PileType.Deck, clonedBy: this));
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

		bool wasGoldStolenBack = GoldRewardWasStolenBackField?.GetValue(reward) is true;
		await DuplicateGoldAmount(player, reward.Amount, wasGoldStolenBack);
	}

	private async Task DuplicateGoldAmount(Player player, int amount, bool wasGoldStolenBack)
	{
		if (amount <= 0)
		{
			return;
		}

		Flash();
		await RunWithCommandDuplicationSuppressed(
			() => PlayerCmd.GainGold(amount, player, wasGoldStolenBack));
		TrySyncObtainedGold(amount);
	}

	private async Task DuplicatePotionReward(Player player, PotionReward reward)
	{
		PotionModel? claimedPotion = reward.ClaimedPotion;
		if (claimedPotion == null)
		{
			return;
		}

		await DuplicateObtainedPotion(player, claimedPotion);
	}

	private async Task DuplicateObtainedPotion(Player player, PotionModel sourcePotion)
	{
		PotionModel copy = ModelDb.GetById<PotionModel>(sourcePotion.CanonicalInstance?.Id ?? sourcePotion.Id).ToMutable();
		PotionProcureResult result = await RunWithCommandDuplicationSuppressed(
			() => PotionCmd.TryToProcure(copy, player));
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

		await DuplicateObtainedRelic(player, claimedRelic);
	}

	private async Task DuplicateObtainedRelic(Player player, RelicModel sourceRelic)
	{
		// 复视不再复制海克斯模组自己的符文/遗物(只对原版遗物生效):自定义符文/遗物的获得→转化→联机同步
		// 流程复杂且对多人敏感,重复获得易引发分叉/卡死(玩家实测黑屏的一类来源)。按需求收窄复视作用域为原版遗物。
		// 判据用「该类型来自本模组程序集」,自动覆盖全部 HextechRelicBase 符文与 HextechForgeBase 锻造,无需逐个列举。
		if (sourceRelic.GetType().Assembly == typeof(DoubleVisionRune).Assembly)
		{
			return;
		}

		if (sourceRelic is DustyTome dustyTome)
		{
			await DuplicateDustyTomeAncientCard(player, dustyTome);
			return;
		}

		// 复视不对 Orobas 先古遗物「古老牙齿」「欧洛巴斯之触」生效（不复制它们）：
		// 它们的获得/转化流程不适合被复制（古老牙齿重复获得会因牌组无可转化牌而卡死）。
		// 黄金罗盘（GoldenCompass）同样跳过：其 AfterObtained 会 await RunManager.GenerateMap() 重建全图、
		// 消耗共享 RNG 并改写共享地图。复制会触发第二次 GenerateMap，而联机远端是经两条 fire-and-forget 奖励消息
		// 异步重算，与持有端的严格顺序不一致 → 两端 State.Map/State.Rng 分叉、随后投票/checksum 报"数据不匹配"。
		// 先古遗物本就不该被双倍，复制出第二枚黄金罗盘语义上也不成立。
		if (sourceRelic is ArchaicTooth or TouchOfOrobas or GoldenCompass)
		{
			return;
		}

		RelicModel copy = ModelDb.GetById<RelicModel>(sourceRelic.CanonicalInstance?.Id ?? sourceRelic.Id).ToMutable();
		RelicModel obtained = await RunWithCommandDuplicationSuppressed(
			() => RelicCmd.Obtain(copy, player));
		Flash();
		TrySyncObtainedRelic(obtained);
	}

	private async Task DuplicateDustyTomeAncientCard(Player player, DustyTome sourceTome)
	{
		if (!TryResolveDustyTomeAncientCard(player, sourceTome, out ModelId ancientCardId))
		{
			Log.Warn($"[{ModInfo.Id}][DoubleVision] Failed to resolve Dusty Tome ancient card for duplicated reward.");
			return;
		}

		CardModel card = player.RunState.CreateCard(ModelDb.GetById<CardModel>(ancientCardId), player);
		CardCmd.Upgrade(card);
		CardPileAddResult result = await RunWithCommandDuplicationSuppressed(
			() => CardPileCmd.Add(card, PileType.Deck, clonedBy: this));
		if (!result.success)
		{
			return;
		}

		SaveManager.Instance.MarkCardAsSeen(result.cardAdded);
		TrySyncObtainedCard(result.cardAdded);
		Flash();
		CardCmd.PreviewCardPileAdd(result, 2f);
	}

	private static bool TryResolveDustyTomeAncientCard(Player player, DustyTome sourceTome, out ModelId ancientCardId)
	{
		if (sourceTome.AncientCard is { } sourceAncientCard)
		{
			ancientCardId = sourceAncientCard;
			return true;
		}

		DustyTome fallback = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
		fallback.SetupForPlayer(player);
		if (fallback.AncientCard is { } fallbackAncientCard)
		{
			ancientCardId = fallbackAncientCard;
			return true;
		}

		ancientCardId = ModelId.none;
		return false;
	}

	private async Task DuplicateForgeReward(Player player, HextechForgeChoiceReward reward)
	{
		if (reward.ClaimedForgeId == ModelId.none)
		{
			return;
		}

		// (B1)附魔锻造的 AfterObtained 会开交互式选牌(FromDeckForEnchantment),必须走「持有者开UI+SyncLocalChoice、
		// 远端 WaitForRemoteChoice」的选择同步协议(每次选牌都 ReserveChoiceId)。复制份只能在【本地持有者】这一端
		// 真正开第二次选牌,再靠 ObtainSelectedForge 的 syncObtainedRelic 广播让远端经 RewardSynchronizer 获得这份
		// 锻造并 WaitForRemoteChoice 回放同一选牌——这是原版 HextechForgeChoiceReward.OnSelect(仅选取端运行)的镜像。
		// 缺这道本地闸门时,远端也各自跑 ObtainSelectedForge 开自己的选牌→复制份 choiceId 与持有者错位→远端拿到
		// Index 型结果→AsDeckCards 抛异常(玩家实测黑屏/卡的来源之一)。非本地持有者直接返回,由广播兜底。
		if (!ShouldDuplicateForPlayer(player))
		{
			return;
		}

		RelicModel forge = ModelDb.GetById<RelicModel>(reward.ClaimedForgeId).ToMutable();
		Flash();
		await RunWithCommandDuplicationSuppressed(
			() => HextechForgeGrantHelper.ObtainSelectedForge(player, forge, syncObtainedRelic: true));
	}

	private static DirectCommandRewardScope? BeginDirectCommandReward(Player player)
	{
		if (IsCommandDuplicationSuppressed()
			|| CombatManager.Instance.IsInProgress
			|| !ShouldDuplicateForPlayer(player))
		{
			return null;
		}

		IReadOnlyList<DoubleVisionRune> runes = GetActiveRunes(player);
		if (runes.Count == 0)
		{
			return null;
		}

		int previousDepth = CommandDuplicationSuppressionDepth.Value;
		CommandDuplicationSuppressionDepth.Value = previousDepth + 1;
		return new DirectCommandRewardScope(player, runes, previousDepth);
	}

	private static void RestoreCommandRewardScope(DirectCommandRewardScope scope)
	{
		CommandDuplicationSuppressionDepth.Value = scope.PreviousSuppressionDepth;
	}

	private static bool ShouldDuplicateDirectDeckCard(CardModel card, PileType newPileType, AbstractModel? clonedBy)
	{
		return newPileType == PileType.Deck
			&& clonedBy == null
			&& !IsCommandDuplicationSuppressed()
			&& !CombatManager.Instance.IsInProgress
			&& card.Owner != null
			&& ShouldDuplicateForPlayer(card.Owner);
	}

	private static bool IsCommandDuplicationSuppressed()
	{
		return CommandDuplicationSuppressionDepth.Value > 0;
	}

	private static async Task<T> RunWithCommandDuplicationSuppressed<T>(Func<Task<T>> action)
	{
		int previousDepth = CommandDuplicationSuppressionDepth.Value;
		CommandDuplicationSuppressionDepth.Value = previousDepth + 1;
		try
		{
			return await action();
		}
		finally
		{
			CommandDuplicationSuppressionDepth.Value = previousDepth;
		}
	}

	private static async Task RunWithCommandDuplicationSuppressed(Func<Task> action)
	{
		int previousDepth = CommandDuplicationSuppressionDepth.Value;
		CommandDuplicationSuppressionDepth.Value = previousDepth + 1;
		try
		{
			await action();
		}
		finally
		{
			CommandDuplicationSuppressionDepth.Value = previousDepth;
		}
	}

	private static IReadOnlyList<DoubleVisionRune> GetActiveRunes(Player player)
	{
		if (!ShouldDuplicateForPlayer(player))
		{
			return [];
		}

		return player.Relics
			.OfType<DoubleVisionRune>()
			.Where(static rune => rune.Owner != null)
			.ToList();
	}

	private static bool ShouldDuplicateForPlayer(Player player)
	{
		if (player.Creature.IsDead)
		{
			return false;
		}

		RunManager? runManager = RunManager.Instance;
		INetGameService? netService = runManager?.NetService;
		if (netService != null
			&& netService.Type is NetGameType.Host or NetGameType.Client
			&& netService.IsConnected
			&& !LocalContext.IsMe(player))
		{
			return false;
		}

		return true;
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

	private sealed class DirectCommandRewardScope : RewardDuplicationScope
	{
		public DirectCommandRewardScope(Player player, IReadOnlyList<DoubleVisionRune> runes, int previousSuppressionDepth)
			: base(runes)
		{
			Player = player;
			PreviousSuppressionDepth = previousSuppressionDepth;
		}

		public Player Player { get; }

		public int PreviousSuppressionDepth { get; }

		public decimal GoldAmount { get; set; }

		public bool WasGoldStolenBack { get; set; }
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

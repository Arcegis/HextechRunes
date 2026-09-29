using MegaCrit.Sts2.Core.Context;

namespace HextechRunes;

public sealed partial class DoubleVisionRune
{
	private static DirectCommandRewardScope? BeginDirectCommandReward(Player player)
	{
		if (IsCommandDuplicationSuppressed()
			|| CombatManager.Instance.IsInProgress
			|| !ShouldDuplicateForPlayer(player))
		{
			return null;
		}

		// 事件房不走 Direct 类内联复制。事件选项内的 RelicCmd.Obtain 由 EventOption.Chosen
		// 外层事务捕获,等原 OnChosen Task 返回后再在所有端顺序提交;不在事务内的背景命令不猜测为奖励。
		// 战后奖励屏的翻倍仍走 RelicReward.OnSelect 等 Reward 专用 hook。
		if (player.RunState.CurrentRoom is EventRoom)
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

	private static IReadOnlyList<DoubleVisionRune> GetEventActiveRunes(Player player)
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

	private static bool ShouldDuplicateForPlayer(Player player)
	{
		if (player.Creature.IsDead)
		{
			return false;
		}

		// 联机时只由奖励所属玩家本机复制：原版奖励领取本就只在领取者本机执行，再经
		// RewardSynchronizer 广播给其他端(见下方 Sync* 调用)。这里的 LocalContext 判断跟随原版奖励流程，
		// 共享结果仍由同步消息决定，不属于"用 LocalContext 决定只在本机结算共享效果"。
		if (HextechPlayerContextHelper.IsMultiplayerConnected() && !LocalContext.IsMe(player))
		{
			return false;
		}

		return true;
	}

	private static void TrySyncObtainedCard(CardModel card)
	{
		if (!HextechPlayerContextHelper.IsMultiplayerConnected())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedCard(card);
		}
		catch (Exception ex)
		{
			HextechLog.Error(
				"DoubleVision", $"[DESYNC-RISK] Local duplicated card reward was already granted, "
				+ $"but its multiplayer broadcast failed: card={card.Id.Entry} error={ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedGold(int amount)
	{
		if (!HextechPlayerContextHelper.IsMultiplayerConnected())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(amount);
		}
		catch (Exception ex)
		{
			HextechLog.Error(
				"DoubleVision", $"[DESYNC-RISK] Local duplicated gold reward was already granted, "
				+ $"but its multiplayer broadcast failed: amount={amount} error={ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedPotion(PotionModel potion)
	{
		if (!HextechPlayerContextHelper.IsMultiplayerConnected())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedPotion(potion);
		}
		catch (Exception ex)
		{
			HextechLog.Error(
				"DoubleVision", $"[DESYNC-RISK] Local duplicated potion reward was already granted, "
				+ $"but its multiplayer broadcast failed: potion={potion.Id.Entry} error={ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TrySyncObtainedRelic(RelicModel relic)
	{
		if (!HextechPlayerContextHelper.IsMultiplayerConnected())
		{
			return;
		}

		try
		{
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
		}
		catch (Exception ex)
		{
			HextechLog.Error(
				"DoubleVision", $"[DESYNC-RISK] Local duplicated relic reward was already granted, "
				+ $"but its multiplayer broadcast failed: relic={relic.Id.Entry} error={ex.GetType().Name}: {ex.Message}");
		}
	}
}

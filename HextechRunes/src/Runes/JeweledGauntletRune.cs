namespace HextechRunes;

public sealed class JeweledGauntletRune : HextechRelicBase
{
	private int _replayRollsThisCombat;

	// 按 card 实例分槽存放待定的重打判定结果,而不是单一共享槽位:引擎/UI 可能在同一张牌的
	// ModifyCardPlayCount 与 AfterModifyingCardPlayCount 之间,针对手牌里其它牌再次调用
	// ModifyCardPlayCount(例如出牌次数预测类 UI 轮询),若用单一槽位会被那次调用覆盖,导致
	// AfterModifyingCardPlayCount 认错牌、漏消费序号、漏播特效。
	private readonly Dictionary<CardModel, bool> _pendingReplayRolls = new();

	public override Task BeforeCombatStart()
	{
		ResetReplayRollState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetReplayRollState();
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (Owner == null || card.Owner != Owner)
		{
			_pendingReplayRolls.Remove(card);
			return playCount;
		}

		int ordinal = PeekCombatProcOrdinal(nameof(JeweledGauntletRune), _replayRollsThisCombat);
		bool shouldReplay = HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			33,
			"jeweled-gauntlet-replay",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			ordinal.ToString(),
			TargetKey(target),
			HextechStableRandom.CardKey(card));
		_pendingReplayRolls[card] = shouldReplay;
		return shouldReplay ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (Owner != null && card.Owner == Owner && _pendingReplayRolls.Remove(card, out bool shouldReplay))
		{
			// 只在这个每次出牌保证只触发一次的钩子里消费序号,避免 ModifyCardPlayCount 被引擎重复调用时序号多推进。
			ConsumeCombatProcOrdinal(nameof(JeweledGauntletRune), ref _replayRollsThisCombat);
			if (shouldReplay)
			{
				Flash();
			}
		}
		else
		{
			_pendingReplayRolls.Remove(card);
		}

		return Task.CompletedTask;
	}

	private static string TargetKey(Creature? target)
	{
		return target?.CombatId?.ToString() ?? target?.Side.ToString() ?? "none";
	}

	private void ResetReplayRollState()
	{
		_replayRollsThisCombat = 0;
		_pendingReplayRolls.Clear();
	}
}

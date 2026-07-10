namespace HextechRunes;

public sealed class JeweledGauntletRune : HextechRelicBase
{
	private int _replayRollsThisCombat;
	private CardModel? _pendingReplayRollCard;
	private bool _pendingReplayRollResult;

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
			ClearPendingReplayRoll();
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
		_pendingReplayRollCard = card;
		_pendingReplayRollResult = shouldReplay;
		return shouldReplay ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (Owner != null && card.Owner == Owner && _pendingReplayRollCard == card)
		{
			// 只在这个每次出牌保证只触发一次的钩子里消费序号,避免 ModifyCardPlayCount 被引擎重复调用时序号多推进。
			ConsumeCombatProcOrdinal(nameof(JeweledGauntletRune), ref _replayRollsThisCombat);
			if (_pendingReplayRollResult)
			{
				Flash();
			}
		}

		if (_pendingReplayRollCard == card)
		{
			ClearPendingReplayRoll();
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
		ClearPendingReplayRoll();
	}

	private void ClearPendingReplayRoll()
	{
		_pendingReplayRollCard = null;
		_pendingReplayRollResult = false;
	}
}

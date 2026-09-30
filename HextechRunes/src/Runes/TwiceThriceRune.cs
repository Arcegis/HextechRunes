namespace HextechRunes;

public sealed class TwiceThriceRune : HextechRelicBase
{
	private const int AttacksPerReplay = 3;

	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetAttacksPlayedThisCombat();
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value) % AttacksPerReplay;
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => IsInLiveCombat;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			return GetAttacksPlayedThisCombat();
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisCombat();
		return Task.CompletedTask;
	}

	// 计数口径同原版苦无（Kunai）：持有者攻击牌的每一次 AfterCardPlayed 都计 1，含重放（PlayIndex > 0）与自动打出。
	// 原版 CardModel.GeneratePlayCount 在第一次打出前一次性算出总次数（Hook.ModifyCardPlayCount 按监听者顺序累加，
	// 0.111.0 没有 Late 版本），之后每次打出都推进计数。本张牌这一系列打出（c+1 … c+playCount）会跨过 3 的倍数时追加一次，
	// 追加的那次同样计数。限制：这里只能看到排在本符文之前的监听者累加后的次数——持有者身上的 Power 全部在遗物之前，
	// 同一玩家的遗物按获得顺序；比本符文晚获得的双刀流等 +1 在这里看不到，只在打出后推进计数。
	// 本钩子可能被同一次出牌重复求值（预测轮询），只读计数不改状态。
	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (!IsOwnedAttack(card))
		{
			return playCount;
		}

		return ShouldAddReplay(GetAttacksPlayedThisCombat(), playCount)
			? playCount + 1
			: playCount;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		if (!ShouldUseNetworkCombatHistory())
		{
			_attacksPlayedThisCombat = (_attacksPlayedThisCombat + 1) % AttacksPerReplay;
		}

		InvokeDisplayAmountChanged();
		Flash();
		return Task.CompletedTask;
	}

	internal static bool ShouldAddReplay(int attacksPlayedBefore, int playCount)
	{
		if (playCount <= 0)
		{
			return false;
		}

		int progress = Math.Max(0, attacksPlayedBefore) % AttacksPerReplay;
		return progress + playCount >= AttacksPerReplay;
	}

	private void ResetAttacksPlayedThisCombat()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}

	private int GetAttacksPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory(firstInSeriesOnly: false, includeAutoPlay: true) % AttacksPerReplay
			: _attacksPlayedThisCombat;
	}
}

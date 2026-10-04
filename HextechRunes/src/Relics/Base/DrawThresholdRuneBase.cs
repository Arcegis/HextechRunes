namespace HextechRunes;

/// <summary>
/// "本场战斗每抽 N 张牌触发一次"的符文：单机本地计数；联机从战斗历史回放持有者的抽牌数，
/// 在出牌后与回合开始后补结算跨过的阈值，两端结果一致。阈值读 DynamicVar "CardsNeeded"。
/// </summary>
/// <remarks>
/// 进度的 [SavedProperty] SavedCardsDrawnThisCombat 留在各子类上并转发到 <see cref="SavedDrawProgress"/>：
/// SavedProperty 集合决定联机 net-id 布局，属性的名字、类型和声明位置都是兼容契约。
/// 奖励不做成按 Power 类型参数化的泛型基类：泛型模型里的 Task 钩子重写不得使用类型参数
/// （见 GenericModelHookOverridesDoNotUseTypeParameters），由各子类直接实现。
/// </remarks>
public abstract class DrawThresholdRuneBase : HextechRelicBase
{
	private int _cardsDrawnThisCombat;

	protected int SavedDrawProgress
	{
		get => IsNetworkMultiplayer() ? 0 : GetCardsDrawnThisCombat();
		set
		{
			_cardsDrawnThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	private int CardsNeeded => DynamicVars["CardsNeeded"].IntValue;

	public override bool ShowCounter => IsInLiveCombat;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			int cardsNeeded = CardsNeeded;
			int remainder = GetCardsDrawnThisCombat() % cardsNeeded;
			return remainder == 0 ? cardsNeeded : cardsNeeded - remainder;
		}
	}

	public override Task BeforeCombatStart()
	{
		ResetProgress();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetProgress();
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveDrawProgressFromHistory();
			return;
		}

		_cardsDrawnThisCombat++;
		InvokeDisplayAmountChanged();
		if (_cardsDrawnThisCombat % CardsNeeded == 0)
		{
			await ApplyDrawThresholdReward();
		}
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && cardPlay.Card.Owner == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (ShouldUseNetworkCombatHistory() && player == Owner)
		{
			await ResolveDrawProgressFromHistory();
		}
	}

	/// <summary>每跨过一个阈值调用一次；持有者可能已死亡，由实现自行判断。</summary>
	protected abstract Task ApplyDrawThresholdReward();

	private async Task ResolveDrawProgressFromHistory()
	{
		if (Owner.Creature.IsDead)
		{
			return;
		}

		int cardsDrawn = CountOwnedCardsDrawnFromHistory();
		int previousCardsDrawn = _cardsDrawnThisCombat;
		if (cardsDrawn <= previousCardsDrawn)
		{
			return;
		}

		_cardsDrawnThisCombat = cardsDrawn;
		InvokeDisplayAmountChanged();
		int rewards = CountThresholdCrossings(previousCardsDrawn, cardsDrawn, CardsNeeded);
		for (int i = 0; i < rewards; i++)
		{
			await ApplyDrawThresholdReward();
		}
	}

	private int GetCardsDrawnThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedCardsDrawnFromHistory()
			: _cardsDrawnThisCombat;
	}

	private void ResetProgress()
	{
		_cardsDrawnThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

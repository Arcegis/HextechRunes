namespace HextechRunes;

public sealed class ChainInSleeveRune : HextechRelicBase
{
	private const int CanonicalShivsNeeded = 3;

	private int _shivsPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedShivsPlayedThisCombat
	{
		get => GetShivsPlayedThisCombat() % ShivsNeeded;
		set
		{
			_shivsPlayedThisCombat = Math.Max(0, value) % ShivsNeeded;
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

			int shivsNeeded = ShivsNeeded;
			int remainder = GetShivsPlayedThisCombat() % shivsNeeded;
			return remainder == 0 ? shivsNeeded : shivsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ShivsNeeded", CanonicalShivsNeeded),
		new CardsVar(1)
	];

	private int ShivsNeeded => DynamicVars["ShivsNeeded"].IntValue;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Shiv>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsCountedShivPlay(cardPlay))
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveShivProgressFromHistory();
			return;
		}

		_shivsPlayedThisCombat++;
		await ResolveShivRewards(previousShivsPlayed: _shivsPlayedThisCombat - 1, currentShivsPlayed: _shivsPlayedThisCombat);
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && IsCountedShivPlay(cardPlay))
		{
			await ResolveShivProgressFromHistory();
		}
	}

	private async Task ResolveShivProgressFromHistory()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int shivsPlayed = CountOwnedShivCardsPlayedFromHistory();
		int previousShivsPlayed = _shivsPlayedThisCombat;
		if (shivsPlayed <= previousShivsPlayed)
		{
			return;
		}

		_shivsPlayedThisCombat = shivsPlayed;
		await ResolveShivRewards(previousShivsPlayed, shivsPlayed);
	}

	private async Task ResolveShivRewards(int previousShivsPlayed, int currentShivsPlayed)
	{
		InvokeDisplayAmountChanged();
		int rewards = CountThresholdCrossings(previousShivsPlayed, currentShivsPlayed, ShivsNeeded);
		if (rewards <= 0 || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await AddShivRewardCards(rewards * DynamicVars.Cards.IntValue);
	}

	// 计数口径同原版苦无（Kunai）：持有者小刀的每一次打出都计 1，含重放（PlayIndex > 0）与自动打出；
	// 联机历史计数用同一口径（firstInSeriesOnly: false、includeAutoPlay: true）。
	private bool IsCountedShivPlay(CardPlay cardPlay)
	{
		return cardPlay.Card.Owner == Owner
			&& HextechKnifeHelper.IsShivLike(cardPlay.Card, Owner);
	}

	private int CountOwnedShivCardsPlayedFromHistory()
	{
		return HextechCombatHistoryHelper.CountOwnedCardsPlayed(
			Owner,
			card => HextechKnifeHelper.IsShivLike(card, Owner),
			firstInSeriesOnly: false,
			includeAutoPlay: true);
	}

	private async Task AddShivRewardCards(int count)
	{
		if (Owner?.GetRelic<BigKnifeRune>() != null)
		{
			await AddCardCopiesToCombatHand<SovereignBlade>(count, HextechKnifeHelper.ConfigureBigKnifeBlade);
			return;
		}

		await AddCardCopiesToCombatHand<Shiv>(count);
	}

	private int GetShivsPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedShivCardsPlayedFromHistory()
			: _shivsPlayedThisCombat;
	}

	private void ResetCounter()
	{
		_shivsPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}

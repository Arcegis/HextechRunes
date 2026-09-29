namespace HextechRunes;

public sealed class WhiteHoleCard : HextechOwnerPoolTokenCard
{
	public override string PortraitPath => HextechAssets.WhiteHoleCardPortraitPath;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(2),
		new CardsVar(2)
	];

	public WhiteHoleCard()
		: base(0, CardType.Status, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: true)
	{
	}

	// 与原版 Void 一样由牌自己监听抽牌（牌在战斗牌堆中即是 Hook 监听者）。
	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card != this || Owner == null)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return Owner == null
			? Task.CompletedTask
			: CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1m);
	}
}

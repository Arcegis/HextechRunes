using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class ElicitCard : CardModel
{
	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override OrbEvokeType OrbEvokeType => OrbEvokeType.All;

	public override string PortraitPath => HextechAssets.ElicitCardPortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Evoke)
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(0)
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Innate,
		CardKeyword.Retain,
		CardKeyword.Exhaust
	];

	public ElicitCard()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int orbCount = Owner.PlayerCombatState?.OrbQueue.Orbs.Count ?? 0;
		for (int i = 0; i < orbCount; i++)
		{
			await OrbCmd.EvokeNext(choiceContext, Owner);
		}

		if (DynamicVars.Cards.IntValue > 0)
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(2m);
	}
}

public sealed class TrickMagicCard : CardModel
{
	public override CardPoolModel Pool => IsMutable && Owner != null
		? Owner.Character.CardPool
		: ModelDb.CardPool<TokenCardPool>();

	public override CardPoolModel VisualCardPool => Pool;

	public override string PortraitPath => HextechAssets.TrickMagicCardPortraitPath;

	public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new PowerVar<BufferPower>(1m),
		new DynamicVar("Replays", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>(),
		HoverTipFactory.FromPower<HextechAttackReplayPower>()
	];

	public TrickMagicCard()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
		await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, this);
		await PowerCmd.Apply<HextechAttackReplayPower>(Owner.Creature, DynamicVars["Replays"].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars["BufferPower"].UpgradeValueBy(1m);
	}
}

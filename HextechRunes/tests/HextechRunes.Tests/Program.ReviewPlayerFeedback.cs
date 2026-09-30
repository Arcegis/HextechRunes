using HextechRunes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes.Tests;

// 玩家反馈批次（2026-09-30）：重放/自动打出的计数口径、敌方开悟载体、升级形态棱彩化。
internal static partial class Program
{
	[HextechTest]
	private static void RoyalTrialGeneratesOnEveryReplayButNotAutoPlay()
	{
		(HextechEnemyHexContext _, Player owner, Player teammate) = CreatePrismaticEnemyFixture();
		SovereignBlade blade = CreateMutableTestModel<SovereignBlade>();
		blade.Owner = owner;
		Expect(RoyalTrialRune.ShouldGenerateMinions(CreateFeedbackCardPlay(blade, playIndex: 0, playCount: 2, isAutoPlay: false), owner),
			"the first play of Sovereign Blade generates minions");
		Expect(RoyalTrialRune.ShouldGenerateMinions(CreateFeedbackCardPlay(blade, playIndex: 1, playCount: 2, isAutoPlay: false), owner),
			"each replay (PlayIndex > 0) generates minions again");
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateFeedbackCardPlay(blade, playIndex: 0, playCount: 1, isAutoPlay: true), owner),
			"auto-played Sovereign Blade does not generate minions");
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateFeedbackCardPlay(blade, playIndex: 0, playCount: 1, isAutoPlay: false), teammate),
			"a teammate's Sovereign Blade does not trigger the owner's rune");
		StrikeIronclad strike = CreateMutableTestModel<StrikeIronclad>();
		strike.Owner = owner;
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateFeedbackCardPlay(strike, playIndex: 0, playCount: 1, isAutoPlay: false), owner),
			"other attacks do not trigger");
	}

	private static CardPlay CreateFeedbackCardPlay(CardModel card, int playIndex, int playCount, bool isAutoPlay)
	{
		return new CardPlay
		{
			Card = card,
#if STS2_109_OR_NEWER
			Player = card.Owner,
#endif
			Target = null,
			ResultPile = PileType.Discard,
			Resources = new ResourceInfo
			{
				EnergySpent = 0,
				EnergyValue = 0,
				StarsSpent = 0,
				StarValue = 0
			},
			IsAutoPlay = isAutoPlay,
			PlayIndex = playIndex,
			PlayCount = playCount
		};
	}
}

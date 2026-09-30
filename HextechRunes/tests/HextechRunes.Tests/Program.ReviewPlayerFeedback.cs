using System.Reflection;
using HarmonyLib;
using HextechRunes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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

	[HextechTest]
	private static void TwiceThriceReplayThresholdCoversTheWholePlaySeries()
	{
		// 单次打出：第 3、6… 张攻击牌追加一次。
		Expect(!TwiceThriceRune.ShouldAddReplay(0, 1), "first attack is not replayed");
		Expect(!TwiceThriceRune.ShouldAddReplay(1, 1), "second attack is not replayed");
		Expect(TwiceThriceRune.ShouldAddReplay(2, 1), "third attack is replayed");
		Expect(TwiceThriceRune.ShouldAddReplay(5, 1), "sixth attack is replayed");
		// 双刀流 +1 排在接二连三之前时看到 playCount=2：这一系列打出里任何一次落在 3 的倍数上都追加一次。
		Expect(!TwiceThriceRune.ShouldAddReplay(0, 2), "plays 1-2 do not reach the third attack");
		Expect(TwiceThriceRune.ShouldAddReplay(1, 2), "plays 2-3 reach the third attack");
		Expect(TwiceThriceRune.ShouldAddReplay(2, 2), "plays 3-4 reach the third attack");
		Expect(TwiceThriceRune.ShouldAddReplay(0, 3), "plays 1-3 reach the third attack");
		Expect(!TwiceThriceRune.ShouldAddReplay(2, 0), "a card that will not be played is never replayed");
	}

	[HextechTest]
	private static void TwiceThriceCountsReplaysAndAutoPlaysLikeKunai()
	{
		(HextechEnemyHexContext _, Player owner, Player teammate) = CreatePrismaticEnemyFixture();
		TwiceThriceRune rune = CreateMutableTestModel<TwiceThriceRune>();
		rune.Owner = owner;
		StrikeIronclad strike = CreateMutableTestModel<StrikeIronclad>();
		strike.Owner = owner;
		StrikeIronclad foreign = CreateMutableTestModel<StrikeIronclad>();
		foreign.Owner = teammate;

		PlayFeedbackCard(rune, strike, playIndex: 0, playCount: 2, isAutoPlay: false);
		PlayFeedbackCard(rune, strike, playIndex: 1, playCount: 2, isAutoPlay: false);
		Equal(2, rune.SavedAttacksPlayedThisCombat, "a replay (PlayIndex=1) advances the count");
		PlayFeedbackCard(rune, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
		Equal(2, rune.SavedAttacksPlayedThisCombat, "a teammate's attack does not advance the count");

		// 旧 bug：计数停在 2 时，自动打出的攻击牌每张都被多打一次而计数不推进。
		Equal(2, rune.ModifyCardPlayCount(strike, null, 1), "an auto-played third attack is replayed");
		Equal(2, rune.ModifyCardPlayCount(strike, null, 1), "the play-count hook is read-only when polled again");
		PlayFeedbackCard(rune, strike, playIndex: 0, playCount: 2, isAutoPlay: true);
		PlayFeedbackCard(rune, strike, playIndex: 1, playCount: 2, isAutoPlay: true);
		Equal(1, rune.SavedAttacksPlayedThisCombat, "both plays of the auto-played attack advance the count, including the added replay");
		Equal(1, rune.ModifyCardPlayCount(strike, null, 1), "the next auto-played attack is no longer replayed");

		// 双刀流在前时看到 playCount=2：计数 1 → 打出 2、3，第 3 张追加一次，共打出 3 次。
		Equal(3, rune.ModifyCardPlayCount(strike, null, 2), "Dual Wield's extra play reaching the third attack adds one replay");
		for (int playIndex = 0; playIndex < 3; playIndex++)
		{
			PlayFeedbackCard(rune, strike, playIndex, playCount: 3, isAutoPlay: false);
		}

		Equal(1, rune.SavedAttacksPlayedThisCombat, "all three plays are counted (1 + 3 = 4)");
		// 双刀流排在接二连三之后（后获得）：这里只看到 1 次（计数 1 → 2）不追加；之后双刀流 +1 让第二次打出落在第 3 张上，也不再追加。
		Equal(1, rune.ModifyCardPlayCount(strike, null, 1), "Dual Wield ordered after this rune is not seen, so no replay is added");
	}

	[HextechTest]
	private static void LightEmUpCountsReplaysAndAutoPlays()
	{
		(HextechEnemyHexContext _, Player owner, Player teammate) = CreatePrismaticEnemyFixture();
		LightEmUpRune rune = CreateMutableTestModel<LightEmUpRune>();
		rune.Owner = owner;
		StrikeIronclad strike = CreateMutableTestModel<StrikeIronclad>();
		strike.Owner = owner;
		StrikeIronclad foreign = CreateMutableTestModel<StrikeIronclad>();
		foreign.Owner = teammate;

		PlayFeedbackCard(rune, strike, playIndex: 0, playCount: 2, isAutoPlay: false);
		PlayFeedbackCard(rune, strike, playIndex: 1, playCount: 2, isAutoPlay: false);
		Equal(2, rune.SavedAttacksPlayedThisCombat, "a replay (PlayIndex=1) advances the volley progress");
		PlayFeedbackCard(rune, strike, playIndex: 0, playCount: 1, isAutoPlay: true);
		Equal(3, rune.SavedAttacksPlayedThisCombat, "an auto-played attack advances the volley progress");
		PlayFeedbackCard(rune, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
		Equal(3, rune.SavedAttacksPlayedThisCombat, "a teammate's attack does not advance the volley progress");
	}

	[HextechTest]
	private static void ChainInSleeveCountsReplaysAndAutoPlaysLikeKunai()
	{
		(HextechEnemyHexContext _, Player owner, Player teammate) = CreatePrismaticEnemyFixture();
		ChainInSleeveRune rune = CreateMutableTestModel<ChainInSleeveRune>();
		rune.Owner = owner;
		Shiv shiv = CreateMutableTestModel<Shiv>();
		shiv.Owner = owner;
		Shiv foreign = CreateMutableTestModel<Shiv>();
		foreign.Owner = teammate;
		FieldInfo counter = typeof(ChainInSleeveRune).GetField("_shivsPlayedThisCombat", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(ChainInSleeveRune), "_shivsPlayedThisCombat");

		PlayFeedbackCard(rune, shiv, playIndex: 1, playCount: 2, isAutoPlay: false);
		Equal(1, (int)counter.GetValue(rune)!, "a replayed Shiv (PlayIndex=1) is counted");
		PlayFeedbackCard(rune, shiv, playIndex: 0, playCount: 1, isAutoPlay: true);
		Equal(2, (int)counter.GetValue(rune)!, "an auto-played Shiv is counted");
		PlayFeedbackCard(rune, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
		Equal(2, (int)counter.GetValue(rune)!, "a teammate's Shiv is not counted");

		// 联机路径读战斗历史，必须与本地计数同口径：重放与自动打出都计，队友的不计。
		MethodInfo historyCount = typeof(ChainInSleeveRune).GetMethod("CountOwnedShivCardsPlayedFromHistory", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(ChainInSleeveRune), "CountOwnedShivCardsPlayedFromHistory");
		CombatHistory history = CombatManager.Instance.History;
		List<CombatHistoryEntry> entries = (List<CombatHistoryEntry>)AccessTools.Field(typeof(CombatHistory), "_entries").GetValue(history)!;
		Expect(entries.Count == 0, "combat history starts empty in tests");
		try
		{
			foreach ((Shiv card, int playIndex, bool isAutoPlay) in new[] { (shiv, 0, false), (shiv, 1, false), (shiv, 0, true), (foreign, 0, false) })
			{
				entries.Add(new CardPlayFinishedEntry(CreateFeedbackCardPlay(card, playIndex, playCount: 2, isAutoPlay), 1, CombatSide.Player, history, []));
			}

			Equal(3, (int)historyCount.Invoke(rune, null)!, "history count includes replays and auto-plays of the owner's Shivs only");
		}
		finally
		{
			history.Clear();
		}
	}

	private static void PlayFeedbackCard(RelicModel rune, CardModel card, int playIndex, int playCount, bool isAutoPlay)
	{
		rune.AfterCardPlayed(null!, CreateFeedbackCardPlay(card, playIndex, playCount, isAutoPlay)).GetAwaiter().GetResult();
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

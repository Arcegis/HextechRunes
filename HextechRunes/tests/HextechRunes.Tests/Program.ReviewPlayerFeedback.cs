using System.Reflection;
using HarmonyLib;
using HextechRunes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

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
		Expect(RoyalTrialRune.ShouldGenerateMinions(CreateCardPlay(blade, playIndex: 0, playCount: 2, isAutoPlay: false), owner),
			"the first play of Sovereign Blade generates minions");
		Expect(RoyalTrialRune.ShouldGenerateMinions(CreateCardPlay(blade, playIndex: 1, playCount: 2, isAutoPlay: false), owner),
			"each replay (PlayIndex > 0) generates minions again");
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateCardPlay(blade, playIndex: 0, playCount: 1, isAutoPlay: true), owner),
			"auto-played Sovereign Blade does not generate minions");
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateCardPlay(blade, playIndex: 0, playCount: 1, isAutoPlay: false), teammate),
			"a teammate's Sovereign Blade does not trigger the owner's rune");
		StrikeIronclad strike = CreateMutableTestModel<StrikeIronclad>();
		strike.Owner = owner;
		Expect(!RoyalTrialRune.ShouldGenerateMinions(CreateCardPlay(strike, playIndex: 0, playCount: 1, isAutoPlay: false), owner),
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

		// 计数读原版战斗完成历史（单机与联机同一口径），这里直接往历史写 CardPlayFinished 条目。
		WithFeedbackCombatHistory((history, entries) =>
		{
			RecordFeedbackPlayFinished(history, entries, strike, playIndex: 0, playCount: 2, isAutoPlay: false);
			RecordFeedbackPlayFinished(history, entries, strike, playIndex: 1, playCount: 2, isAutoPlay: false);
			Equal(2, rune.DisplayAmount, "a replay (PlayIndex=1) advances the count");
			RecordFeedbackPlayFinished(history, entries, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
			Equal(2, rune.DisplayAmount, "a teammate's attack does not advance the count");

			// 旧 bug：计数停在 2 时，自动打出的攻击牌每张都被多打一次而计数不推进。
			Equal(2, rune.ModifyCardPlayCount(strike, null, 1), "an auto-played third attack is replayed");
			Equal(2, rune.ModifyCardPlayCount(strike, null, 1), "the play-count hook is read-only when polled again");
			RecordFeedbackPlayFinished(history, entries, strike, playIndex: 0, playCount: 2, isAutoPlay: true);
			RecordFeedbackPlayFinished(history, entries, strike, playIndex: 1, playCount: 2, isAutoPlay: true);
			Equal(1, rune.DisplayAmount, "both plays of the auto-played attack advance the count, including the added replay");
			Equal(1, rune.ModifyCardPlayCount(strike, null, 1), "the next auto-played attack is no longer replayed");

			// 双刀流在前时看到 playCount=2：计数 1 → 打出 2、3，第 3 张追加一次，共打出 3 次。
			Equal(3, rune.ModifyCardPlayCount(strike, null, 2), "Dual Wield's extra play reaching the third attack adds one replay");
			for (int playIndex = 0; playIndex < 3; playIndex++)
			{
				RecordFeedbackPlayFinished(history, entries, strike, playIndex, playCount: 3, isAutoPlay: false);
			}

			Equal(1, rune.DisplayAmount, "all three plays are counted (1 + 3 = 4)");
			// 双刀流排在接二连三之后（后获得）：这里只看到 1 次（计数 1 → 2）不追加；之后双刀流 +1 让第二次打出落在第 3 张上，也不再追加。
			Equal(1, rune.ModifyCardPlayCount(strike, null, 1), "Dual Wield ordered after this rune is not seen, so no replay is added");
			Equal(0, rune.SavedAttacksPlayedThisCombat, "the legacy saved counter is a compatibility placeholder");
		});
		Equal(0, rune.DisplayAmount, "the count is empty once combat history is cleared");
	}

	// 审查复现：一呼百应（先获得）在外层攻击牌的 AfterCardPlayed 里嵌套自动打出攻击牌。原版先记外层的 CardPlayFinished、
	// 再依次派发 AfterCardPlayed，所以嵌套那张求 ModifyCardPlayCount 时外层已在历史里、接二连三的 AfterCardPlayed 还没执行。
	// 单机与联机必须给出同样的追加次数。
	[HextechTest]
	private static void TwiceThriceNestedAutoPlayMatchesInSingleAndMultiplayer()
	{
		(HextechEnemyHexContext _, Player owner, Player _) = CreatePrismaticEnemyFixture();
		TwiceThriceRune rune = CreateMutableTestModel<TwiceThriceRune>();
		rune.Owner = owner;
		StrikeIronclad outer = CreateMutableTestModel<StrikeIronclad>();
		outer.Owner = owner;
		StrikeIronclad nested = CreateMutableTestModel<StrikeIronclad>();
		nested.Owner = owner;

		// 起始计数 1：外层是第 2 张，嵌套那张是第 3 张 → 追加。起始计数 2：外层第 3 张、嵌套第 4 张 → 不追加。
		foreach ((int startingCount, int expectedPlayCount) in new[] { (1, 2), (2, 1) })
		{
			foreach (NetGameType mode in new[] { NetGameType.Singleplayer, NetGameType.Host, NetGameType.Client })
			{
				WithFeedbackNetGameType(mode, () => WithFeedbackCombatHistory((history, entries) =>
				{
					for (int i = 0; i < startingCount; i++)
					{
						RecordFeedbackPlayFinished(history, entries, outer, playIndex: 0, playCount: 1, isAutoPlay: false);
					}

					Equal(startingCount, rune.DisplayAmount, $"starting count ({mode}, start {startingCount})");
					Equal(startingCount == 2 ? 2 : 1, rune.ModifyCardPlayCount(outer, null, 1), $"outer attack play count ({mode}, start {startingCount})");
					int outerPlayCount = startingCount == 2 ? 2 : 1;
					// 外层只打第一次就进入嵌套：此刻外层第一次打出已记完成历史，接二连三的 AfterCardPlayed 尚未执行。
					RecordFeedbackPlayFinished(history, entries, outer, playIndex: 0, playCount: outerPlayCount, isAutoPlay: false);
					Equal(expectedPlayCount, rune.ModifyCardPlayCount(nested, null, 1), $"nested auto-played attack ({mode}, start {startingCount})");
				}));
			}
		}
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
		FieldInfo resolved = typeof(ChainInSleeveRune).GetField("_shivsPlayedThisCombat", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(ChainInSleeveRune), "_shivsPlayedThisCombat");

		// 单机与联机都读战斗完成历史：重放与自动打出都计，队友的不计。
		WithFeedbackCombatHistory((history, entries) =>
		{
			RecordFeedbackPlayFinished(history, entries, shiv, playIndex: 1, playCount: 2, isAutoPlay: false);
			PlayFeedbackCard(rune, shiv, playIndex: 1, playCount: 2, isAutoPlay: false);
			Equal(1, (int)resolved.GetValue(rune)!, "a replayed Shiv (PlayIndex=1) is counted");
			RecordFeedbackPlayFinished(history, entries, shiv, playIndex: 0, playCount: 1, isAutoPlay: true);
			PlayFeedbackCard(rune, shiv, playIndex: 0, playCount: 1, isAutoPlay: true);
			Equal(2, (int)resolved.GetValue(rune)!, "an auto-played Shiv is counted");
			RecordFeedbackPlayFinished(history, entries, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
			PlayFeedbackCard(rune, foreign, playIndex: 0, playCount: 1, isAutoPlay: false);
			Equal(2, (int)resolved.GetValue(rune)!, "a teammate's Shiv is not counted");
			Equal(1, rune.DisplayAmount, "one more Shiv is needed");
		});
	}

	// 嵌套自动打出小刀：外层小刀已记完成历史、本符文对外层的 AfterCardPlayed 还没执行时，嵌套那张的 AfterCardPlayed
	// 就把两张一起结算；随后外层的 AfterCardPlayed 不再推进。单机与联机同一时点。
	[HextechTest]
	private static void ChainInSleeveNestedAutoPlayResolvesAtTheSameMomentInSingleAndMultiplayer()
	{
		(HextechEnemyHexContext _, Player owner, Player _) = CreatePrismaticEnemyFixture();
		ChainInSleeveRune rune = CreateMutableTestModel<ChainInSleeveRune>();
		rune.Owner = owner;
		Shiv outer = CreateMutableTestModel<Shiv>();
		outer.Owner = owner;
		Shiv nested = CreateMutableTestModel<Shiv>();
		nested.Owner = owner;
		FieldInfo resolved = typeof(ChainInSleeveRune).GetField("_shivsPlayedThisCombat", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(ChainInSleeveRune), "_shivsPlayedThisCombat");

		foreach (NetGameType mode in new[] { NetGameType.Singleplayer, NetGameType.Host })
		{
			rune.BeforeCombatStart().GetAwaiter().GetResult();
			WithFeedbackNetGameType(mode, () => WithFeedbackCombatHistory((history, entries) =>
			{
				RecordFeedbackPlayFinished(history, entries, outer, playIndex: 0, playCount: 1, isAutoPlay: false);
				RecordFeedbackPlayFinished(history, entries, nested, playIndex: 0, playCount: 1, isAutoPlay: true);
				PlayFeedbackCard(rune, nested, playIndex: 0, playCount: 1, isAutoPlay: true);
				Equal(2, (int)resolved.GetValue(rune)!, $"the nested Shiv's hook resolves both Shivs ({mode})");
				PlayFeedbackCard(rune, outer, playIndex: 0, playCount: 1, isAutoPlay: false);
				Equal(2, (int)resolved.GetValue(rune)!, $"the outer Shiv's later hook does not advance again ({mode})");
			}));
		}
	}

	[HextechTest]
	private static void EnemyEnlightenmentUsesItsOwnIconCarrier()
	{
		MonsterHexRegistration row = HextechMonsterHexRegistry.Registrations.Single(registration => registration.Kind == MonsterHexKind.Enlightenment);
		Equal(typeof(EnlightenmentHex), row.IconRelicType, "enemy Enlightenment shows its own icon carrier");
		Expect(!HextechPlayerRuneRegistry.Registrations.Any(registration => registration.Type == typeof(EnlightenmentHex)),
			"the enemy carrier never enters the player pool");

		EnlightenmentHex carrier = new();
		Expect(HextechCatalog.IsHextechEnemyHexIconRelic(carrier), "the carrier is recognised as an enemy hex icon relic");
		Equal("res://HextechRunes/images/relics/enlightenmentHex.png", HextechAssets.TryGetCustomRelicIconPath(carrier), "enemy icon path");
		Equal("res://HextechRunes/images/relics/enlightenmentRune.png", HextechAssets.TryGetCustomRelicIconPath(new EnlightenmentRune()), "player icon path is unchanged");
	}

	[HextechTest]
	private static void FormUpgradeRunesAreAllPrismatic()
	{
		(Type Type, PlayerRuneCharacterPool Pool)[] forms =
		[
			(typeof(DemonFormUpgradeRune), PlayerRuneCharacterPool.Ironclad),
			(typeof(SerpentFormUpgradeRune), PlayerRuneCharacterPool.Silent),
			(typeof(VoidFormUpgradeRune), PlayerRuneCharacterPool.Regent),
			(typeof(EchoFormUpgradeRune), PlayerRuneCharacterPool.Defect),
			(typeof(ReaperFormUpgradeRune), PlayerRuneCharacterPool.Necrobinder)
		];
		foreach ((Type type, PlayerRuneCharacterPool pool) in forms)
		{
			PlayerRuneRegistration registration = HextechPlayerRuneRegistry.Registrations.Single(row => row.Type == type);
			Equal(HextechRarityTier.Prismatic, registration.Rarity, type.Name + " rarity");
			Equal<PlayerRuneCharacterPool?>(pool, registration.CharacterPool, type.Name + " character pool");
		}
	}

	[HextechTest]
	private static void InitialForgeGrantResumesOnlyTheRemainingForges()
	{
		Equal(3, InitialForgeGrantRune.ResolveCompletedForgeCount(6, savedCompleted: 3, forgesObtainedAfterRune: 0),
			"6 forges with 3 done resumes from the 4th, granting 3 more");
		Equal(3, InitialForgeGrantRune.ResolveCompletedForgeCount(6, savedCompleted: 3, forgesObtainedAfterRune: 3), "saved and inferred progress agree");
		Equal(3, InitialForgeGrantRune.ResolveCompletedForgeCount(6, savedCompleted: 0, forgesObtainedAfterRune: 3),
			"a legacy save without the completed count infers it from the inventory");
		Equal(2, InitialForgeGrantRune.ResolveCompletedForgeCount(2, savedCompleted: 0, forgesObtainedAfterRune: 5), "inferred progress is capped at the grant size");
		Equal(6, InitialForgeGrantRune.ResolveCompletedForgeCount(6, savedCompleted: 9, forgesObtainedAfterRune: 0), "saved progress is capped at the grant size");
		Equal(0, InitialForgeGrantRune.ResolveCompletedForgeCount(6, savedCompleted: -1, forgesObtainedAfterRune: 0), "negative saved progress is ignored");

		StatsOnStatsOnStatsRune rune = CreateMutableTestModel<StatsOnStatsOnStatsRune>();
		Equal(0, rune.SavedInitialForgeGrantCompletedCount, "completed count defaults to zero");
		rune.SavedInitialForgeGrantCompletedCount = 3;
		Equal(3, rune.SavedInitialForgeGrantCompletedCount, "completed count is saveable");
	}

	[HextechTest]
	private static void InitialForgeGrantInfersLegacyProgressFromRelicOrder()
	{
		(HextechEnemyHexContext _, Player owner, Player _) = CreatePrismaticEnemyFixture();
		StatsOnStatsOnStatsRune rune = CreateMutableTestModel<StatsOnStatsOnStatsRune>();
		rune.FloorAddedToDeck = 5;
		RelicModel earlierForge = CreateFeedbackRelic<StrengthForge>(floor: 5);
		RelicModel otherRune = CreateFeedbackRelic<TwiceThriceRune>(floor: 5);
		RelicModel laterFloorForge = CreateFeedbackRelic<UpgradeForge>(floor: 6);
		List<RelicModel> relics =
		[
			earlierForge,
			rune,
			CreateFeedbackRelic<DexterityForge>(floor: 5),
			otherRune,
			CreateFeedbackRelic<LifeForge>(floor: 5),
			CreateFeedbackRelic<UpgradeForge>(floor: 5),
			laterFloorForge
		];
		Equal(3, InitialForgeGrantRune.CountForgesObtainedAfter(relics, rune),
			"only forges after the rune and on its floor count; earlier forges, other relics and later floors do not");
		Equal(0, InitialForgeGrantRune.CountForgesObtainedAfter(relics, CreateMutableTestModel<StatsRune>()), "a rune missing from the inventory infers nothing");

		// 背包已补齐时恢复直接完成，不再打开锻造器选择。
		List<RelicModel> complete = [rune];
		foreach (Type forgeType in new[] { typeof(StrengthForge), typeof(DexterityForge), typeof(LifeForge), typeof(UpgradeForge), typeof(StrengthForge), typeof(DexterityForge) })
		{
			RelicModel forge = (RelicModel)Activator.CreateInstance(forgeType)!;
			SetAutoProperty(forge, nameof(AbstractModel.IsMutable), true);
			forge.FloorAddedToDeck = 5;
			complete.Add(forge);
		}

		FieldInfo relicsField = AccessTools.Field(typeof(Player), "_relics");
		object? previousRelics = relicsField.GetValue(owner);
		try
		{
			relicsField.SetValue(owner, complete);
			rune.Owner = owner;
			rune.SavedInitialForgeGrantPending = true;
			rune.SavedInitialForgeGrantCompletedCount = 0;
			Expect(rune.ResumePendingInitialForgeGrant().GetAwaiter().GetResult(), "a legacy pending grant whose forges are all in the inventory completes");
			Expect(!rune.SavedInitialForgeGrantPending, "the pending flag is cleared");
			Equal(0, rune.SavedInitialForgeGrantCompletedCount, "the completed count is cleared with the flag");
		}
		finally
		{
			relicsField.SetValue(owner, previousRelics);
		}
	}

	private static RelicModel CreateFeedbackRelic<T>(int floor)
		where T : RelicModel, new()
	{
		T relic = CreateMutableTestModel<T>();
		relic.FloorAddedToDeck = floor;
		return relic;
	}

	private static void WithFeedbackCombatHistory(Action<CombatHistory, List<CombatHistoryEntry>> body)
	{
		CombatHistory history = CombatManager.Instance.History;
		List<CombatHistoryEntry> entries = (List<CombatHistoryEntry>)AccessTools.Field(typeof(CombatHistory), "_entries").GetValue(history)!;
		Expect(entries.Count == 0, "combat history starts empty in tests");
		try
		{
			body(history, entries);
		}
		finally
		{
			history.Clear();
		}
	}

	private static void RecordFeedbackPlayFinished(CombatHistory history, List<CombatHistoryEntry> entries, CardModel card, int playIndex, int playCount, bool isAutoPlay)
	{
		entries.Add(new CardPlayFinishedEntry(CreateCardPlay(card, playIndex, playCount, isAutoPlay), 1, CombatSide.Player, history, []));
	}

	// 临时把 RunManager.NetService 换成只回答 Type 的代理，模拟单机/房主/客机；结束后还原。
	private static void WithFeedbackNetGameType(NetGameType type, Action body)
	{
		PropertyInfo property = AccessTools.Property(typeof(RunManager), nameof(RunManager.NetService))
			?? throw new MissingMemberException(nameof(RunManager), nameof(RunManager.NetService));
		object? previous = property.GetValue(RunManager.Instance);
		INetGameService proxy = DispatchProxy.Create<INetGameService, FeedbackNetGameServiceProxy>();
		((FeedbackNetGameServiceProxy)(object)proxy).GameType = type;
		try
		{
			property.SetValue(RunManager.Instance, proxy);
			Equal(type is NetGameType.Host or NetGameType.Client, HextechPlayerContextHelper.IsNetworkMultiplayerRun(), $"simulated net mode {type}");
			body();
		}
		finally
		{
			property.SetValue(RunManager.Instance, previous);
		}
	}

	public class FeedbackNetGameServiceProxy : DispatchProxy
	{
		internal NetGameType GameType { get; set; }

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod?.Name == "get_" + nameof(INetGameService.Type))
			{
				return GameType;
			}

			Type? returnType = targetMethod?.ReturnType;
			return returnType != null && returnType.IsValueType && returnType != typeof(void)
				? Activator.CreateInstance(returnType)
				: null;
		}
	}

	private static void PlayFeedbackCard(RelicModel rune, CardModel card, int playIndex, int playCount, bool isAutoPlay)
	{
		rune.AfterCardPlayed(null!, CreateCardPlay(card, playIndex, playCount, isAutoPlay)).GetAwaiter().GetResult();
	}
}

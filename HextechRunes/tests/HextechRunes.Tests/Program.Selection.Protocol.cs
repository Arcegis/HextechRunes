using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using HextechRunes;
using FormVfxKind = HextechRunes.HextechFormVfxSafetyHooks.FormVfxKind;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void ActRollRoundTripKeepsHostSnapshot()
	{
		ModelId disabledRune = HextechCatalog.GetConfigurablePlayerRuneIds()
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.First();
		HashSet<string> disabledIds = [ disabledRune.Entry ];
		string disabledForgeId = HextechCatalog.GetAllForgeTypes()
			.Select(ModelDb.GetId)
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.First()
			.Entry;
		HextechRunConfigurationSnapshot snapshot = HextechRuneConfiguration.GetDefaultSnapshot() with
		{
			PlayerHexCountsByAct = [ 2, 0, 8 ],
			EnemyHexCountsByAct = [ -1, 7, 3 ],
			DisabledPlayerRuneIds = disabledIds,
			DisabledMonsterHexIds = [ MonsterHexKind.FrostWraith.ToString() ],
			DisabledForgeIds = [ disabledForgeId ],
			RuneRarityWeightsByAct =
			[
				new HextechRarityWeights(4, 5, 6),
				new HextechRarityWeights(7, 8, 9),
				new HextechRarityWeights(10, 11, 12)
			],
			PreventConsecutiveSilverRunes = false,
			GoldenRerollChancePercent = 37,
			ForgeRarityWeights = new HextechForgeRarityWeights(9, 10, 11),
			RandomForgeShopPrice = 123,
			PlayerRuneRerollLimit = 8,
			MonsterHexRerollLimit = HextechRuneConfiguration.InfiniteRerollLimit
		};

		PlayerChoiceResult result = HextechChoiceCodec.CreateActRoll(
			actIndex: 1,
			rarity: HextechRarityTier.Gold,
			monsterHex: MonsterHexKind.ShrinkRay,
			hostUsesBetterMultiplayerScaling: true,
			enemyHexCountsByAct: [ -1, 7, 3 ],
			disabledPlayerRuneIds: disabledIds,
			runConfigurationSnapshot: snapshot);

		Expect(HextechChoiceCodec.TryDecodeActRoll(
			result,
			expectedActIndex: 1,
			out HextechRarityTier rarity,
			out MonsterHexKind? monsterHex,
			out bool hostUsesBetterMultiplayerScaling,
			out int[] enemyHexCountsByAct,
			out HashSet<string> decodedDisabledIds,
			out HextechRunConfigurationSnapshot decodedSnapshot), "act roll should decode");

		Equal(HextechRarityTier.Gold, rarity, "rarity");
		Equal(MonsterHexKind.ShrinkRay, monsterHex, "monster hex");
		Equal(true, hostUsesBetterMultiplayerScaling, "host scaling flag");
		SequenceEqual(new[] { 0, 6, 3 }, enemyHexCountsByAct, "enemy count snapshot");
		Expect(decodedDisabledIds.Contains(disabledRune.Entry), "disabled player rune id should round-trip");
		SequenceEqual(new[] { 2, 0, 6 }, decodedSnapshot.PlayerHexCountsByAct, "player count snapshot");
		SetEqual([ MonsterHexKind.FrostWraith.ToString() ], decodedSnapshot.DisabledMonsterHexIds, "disabled monster hex ids");
		SetEqual([ disabledForgeId ], decodedSnapshot.DisabledForgeIds, "disabled forge ids");
		Equal(123, decodedSnapshot.RandomForgeShopPrice, "forge shop price");
		Equal(8, decodedSnapshot.PlayerRuneRerollLimit, "player reroll limit");
		Equal(HextechRuneConfiguration.InfiniteRerollLimit, decodedSnapshot.MonsterHexRerollLimit, "monster reroll limit");
		SequenceEqual(
			new[]
			{
				new HextechRarityWeights(4, 5, 6),
				new HextechRarityWeights(7, 8, 9),
				new HextechRarityWeights(10, 11, 12)
			},
			decodedSnapshot.RuneRarityWeightsByAct,
			"rune rarity weights by act");
		Equal(false, decodedSnapshot.PreventConsecutiveSilverRunes, "prevent consecutive Silver toggle");
		Equal(37, decodedSnapshot.GoldenRerollChancePercent, "golden reroll chance");
		Equal(10, decodedSnapshot.ForgeRarityWeights.Gold, "forge rarity weight");
		Expect(!HextechChoiceCodec.TryDecodeActRoll(result, 0, out _, out _, out _, out _, out _), "wrong act should be rejected");
	}

	[HextechTest]
	private static void RuneSelectionRoundTripRequiresMatchingActAndOrdinal()
	{
		RelicModel[] finalOptions = CreateRuneSelectionTestOptions(3);
		ModelId[] finalOptionIds = finalOptions
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToArray();
		PlayerChoiceResult result = HextechChoiceCodec.CreateRuneSelection(
			actIndex: 1,
			choiceOrdinal: 2,
			selectedIndex: 1,
			rerollHistory: [ 2, 0 ],
			finalOptions);

		Expect(HextechChoiceCodec.TryDecodeRuneSelection(result, 1, 2, out RuneSelectionPayload? decoded), "matching rune selection should decode");
		Equal(1, decoded.SelectedIndex, "selected index");
		SequenceEqual(new[] { 2, 0 }, decoded.RerollHistory, "reroll history");
		SequenceEqual(finalOptionIds, decoded.FinalOptionIds, "final option ids");
	}

	[HextechTest]
	private static void RuneSelectionRejectsWrongActOrOrdinal()
	{
		PlayerChoiceResult result = HextechChoiceCodec.CreateRuneSelection(
			actIndex: 1,
			choiceOrdinal: 2,
			selectedIndex: 0,
			rerollHistory: [],
			CreateRuneSelectionTestOptions(3));

		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(result, 0, 2, out _), "wrong rune selection act should be rejected");
		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(result, 1, 1, out _), "wrong rune selection ordinal should be rejected");

		PlayerChoiceResult malformed = PlayerChoiceResult.FromIndexes(new List<int> { Magic, ChoiceKindRuneSelection, 1, 2, 0, 2, 0 });
		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(malformed, 1, 2, out _), "malformed rune selection should be rejected");
	}

	[HextechTest]
	private static void RuneSelectionSynchronizesIntermediateSeenCandidates()
	{
		RelicModel[] candidates = CreateRuneSelectionTestOptions(3);
		ModelId initial = candidates[0].Id;
		ModelId intermediate = candidates[1].Id;
		ModelId final = candidates[2].Id;
		HashSet<ModelId> offered = [ initial, intermediate ];
		HashSet<ModelId> exclusions = new(offered);
		// 同一槽位 A -> B -> C;未见池耗尽会清空重随排除集合,本次已展示历史仍须传给远端。
		exclusions.Clear();
		exclusions.Add(final);
		HextechWeightedRuneOptions finalOptions = new([ candidates[2] ], 170);
		PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [ 0, 0 ], finalOptions, offered);
		Expect(
			HextechChoiceCodec.TryDecodeRuneSelection(choice, 1, 2, out RuneSelectionPayload? decoded),
			"seen history decodes with consecutive rerolls");
		SequenceEqual(new[] { 0, 0 }, decoded.RerollHistory, "same-slot reroll history");
		SequenceEqual(new[] { final }, decoded.FinalOptionIds, "only C is in final options");
		Expect(
			decoded.SeenOptionIds.ToHashSet().SetEquals(new[] { initial, intermediate, final }),
			"remote receives A and overwritten B after the exclusion pool resets");
		Equal(170, decoded.CharacterWeightPercent, "final weight survives seen history");
		Expect(HextechGeneratedRuneDataCodec.Restore(decoded.GeneratedRuneData, finalOptions), "recipe parser skips seen history");
		PlayerChoiceResult reordered = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [ 0, 0 ], finalOptions, offered.Reverse());
		SequenceEqual(choice.AsIndexes(), reordered.AsIndexes(), "seen history uses canonical ordering");
	}

	[HextechTest]
	private static void RuneSelectionSeenHistoryIncludesSelfPickAndRejectsLegacyPayloads()
	{
		RelicModel[] candidates = CreateRuneSelectionTestOptions(3);
		HextechWeightedRuneOptions finalOptions = new([ candidates[2] ], 170);
		PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [], finalOptions, [ candidates[0].Id, candidates[1].Id ]);
		Expect(
			HextechChoiceCodec.TryDecodeRuneSelection(choice, 1, 2, out RuneSelectionPayload? decoded),
			"self-pick history decodes");
		Equal(0, decoded.SelectedIndex, "self-pick selected index");
		Equal(0, decoded.RerollHistory.Count, "self-pick has no rerolls");
		SequenceEqual(new[] { candidates[2].Id }, decoded.FinalOptionIds, "self-pick carries one final model");
		Expect(
			decoded.SeenOptionIds.ToHashSet().SetEquals(candidates.Select(static relic => relic.Id)),
			"self-pick appends the chosen model even when it was outside the original offers");

		List<int> legacy = [ Magic, ChoiceKindRuneSelection, 1, 2, 0, 0 ];
		HextechStableModelIdListCodec.Append(legacy, decoded.FinalOptionIds);
		HextechRuneWeightCodec.Append(legacy, finalOptions);
		PlayerChoiceResult oldChoice = PlayerChoiceResult.FromIndexes(legacy);
		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(oldChoice, 1, 2, out _), "pre-history payload cannot silently drop intermediate candidates");
	}

	[HextechTest]
	private static void RuneSelectionSeenHistoryPreservesMultipleChunks()
	{
		foreach (int count in new[] { 64, 65, 128, 129, 1025 })
		{
			HextechWeightedRuneOptions options = new([ new GeneratedTestRelic { Data = "recipe:chunked" } ], 170);
			ModelId[] seen = Enumerable.Range(0, count - 1)
				.Select(static index => new ModelId("HEXTECH_TEST", $"SEEN_{index}"))
				.Append(options[0].Id)
				.ToArray();
			PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [], options, seen);
			Expect(
				HextechChoiceCodec.TryDecodeRuneSelection(choice, 1, 2, out RuneSelectionPayload? decoded),
				$"{count} seen IDs decode across chunk boundaries");
			Equal(count, decoded.SeenOptionIds.Count, "no IDs are truncated");
			Expect(decoded.SeenOptionIds.ToHashSet().SetEquals(seen), "every seen ID survives");
			Equal(170, decoded.CharacterWeightPercent, "weight survives chunking");
			RelicModel[] restored = [ new GeneratedTestRelic() ];
			Expect(HextechGeneratedRuneDataCodec.Restore(decoded.GeneratedRuneData, restored), "recipe follows all seen chunks");
			Equal("recipe:chunked", ((GeneratedTestRelic)restored[0]).Data, "recipe survives chunking");
			PlayerChoiceResult reordered = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [], options, seen.Reverse().Concat(seen));
			SequenceEqual(choice.AsIndexes(), reordered.AsIndexes(), "deduplication and ordering are stable across chunks");
		}
	}

	[HextechTest]
	private static void RuneSelectionPreservesLongRerollHistory()
	{
		foreach (int count in new[] { 65, 257 })
		{
			HextechWeightedRuneOptions options = new([ new GeneratedTestRelic { Data = "recipe:rerolled" } ], 170);
			int[] history = Enumerable.Range(0, count).Select(static index => index % 3).ToArray();
			ModelId[] seen = Enumerable.Range(0, count)
				.Select(static index => new ModelId("HEXTECH_TEST", $"REROLL_{index}"))
				.ToArray();
			PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, history, options, seen);
			Expect(HextechChoiceCodec.TryDecodeRuneSelection(choice, 1, 2, out RuneSelectionPayload? decoded), $"{count} rerolls decode");
			SequenceEqual(history, decoded.RerollHistory, "all rerolls retain their original order and count");
			Equal(170, decoded.CharacterWeightPercent, "weight follows long reroll history");
			RelicModel[] restored = [ new GeneratedTestRelic() ];
			Expect(HextechGeneratedRuneDataCodec.Restore(decoded.GeneratedRuneData, restored), "recipe follows long reroll history");
			Equal("recipe:rerolled", ((GeneratedTestRelic)restored[0]).Data, "recipe survives long reroll history");
			foreach (int invalidCount in new[] { -1, int.MaxValue })
			{
				List<int> malformed = choice.AsIndexes().ToList();
				malformed[5] = invalidCount;
				PlayerChoiceResult invalid = PlayerChoiceResult.FromIndexes(malformed);
				Expect(!HextechChoiceCodec.TryDecodeRuneSelection(invalid, 1, 2, out _), "invalid reroll count rejected");
			}
		}
	}

	[HextechTest]
	private static void RuneSelectionSeenHistoryEnforcesProtocolLimits()
	{
		RelicModel[] finalOptions = CreateRuneSelectionTestOptions(1);
		ModelId[] seen = Enumerable.Range(0, HextechStableModelIdListCodec.MaxCount * 2)
			.Select(static index => new ModelId("HEXTECH_TEST", $"SEEN_{index}"))
			.ToArray();
		PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [], finalOptions, seen);
		List<int> payload = choice.AsIndexes().ToList();
		Expect(HextechStableModelIdListCodec.TryDecode(payload, 6, out _, out int seenCursor), "find seen section");
		foreach (int invalidCount in new[] { -1, 0, 1, int.MaxValue })
		{
			List<int> malformed = payload.ToList();
			malformed[seenCursor + 1] = invalidCount;
			Expect(
				!HextechChoiceCodec.TryDecodeRuneSelection(PlayerChoiceResult.FromIndexes(malformed), 1, 2, out _),
				"negative, inconsistent or impossible total is rejected");
		}

		foreach (int invalidCount in new[] { 0, HextechStableModelIdListCodec.MaxCount + 1 })
		{
			List<int> malformed = payload.ToList();
			malformed[seenCursor + 3] = invalidCount;
			Expect(
				!HextechChoiceCodec.TryDecodeRuneSelection(PlayerChoiceResult.FromIndexes(malformed), 1, 2, out _),
				"empty or oversized chunk is rejected");
		}

		List<int> duplicateChunks = [ HextechRuneSeenHistoryCodec.Version, 2 ];
		HextechStableModelIdListCodec.Append(duplicateChunks, [ finalOptions[0].Id ]);
		HextechStableModelIdListCodec.Append(duplicateChunks, [ finalOptions[0].Id ]);
		int cursor = 0;
		Expect(!HextechRuneSeenHistoryCodec.TryRead(duplicateChunks, ref cursor, out _), "duplicates across chunks are rejected");
		Equal(0, cursor, "failed decoding does not consume the caller's cursor");
		List<int> truncated = payload.Take(payload.Count - 1).ToList();
		Expect(
			!HextechChoiceCodec.TryDecodeRuneSelection(PlayerChoiceResult.FromIndexes(truncated), 1, 2, out _),
			"truncated seen data is rejected");
		List<int> trailing = payload.Append(12345).ToList();
		Expect(
			!HextechChoiceCodec.TryDecodeRuneSelection(PlayerChoiceResult.FromIndexes(trailing), 1, 2, out _),
			"unknown trailing payload is rejected");
	}

	[HextechTest]
	private static void ActSelectionAppliedRejectsWrongActOrOrdinal()
	{
		PlayerChoiceResult result = HextechChoiceCodec.CreateActSelectionApplied(2, 3);

		Expect(HextechChoiceCodec.TryDecodeActSelectionApplied(result, 2, 3), "matching act and ordinal should decode");
		Expect(!HextechChoiceCodec.TryDecodeActSelectionApplied(result, 1, 3), "wrong act should be rejected");
		Expect(!HextechChoiceCodec.TryDecodeActSelectionApplied(result, 2, 2), "wrong ordinal should be rejected");

		PlayerChoiceResult malformed = PlayerChoiceResult.FromIndexes(new List<int> { Magic, ChoiceKindActSelectionApplied, 2, 3, 0 });
		Expect(!HextechChoiceCodec.TryDecodeActSelectionApplied(malformed, 2, 3), "missing applied flag should be rejected");
	}

	[HextechTest]
	private static void EnemyHexAdjustmentRoundTripKeepsAllSlots()
	{
		const int OperationToken = 112233;
		EnemyHexAdjustmentPayload source = new(
			ActIndex: 0,
			Sequence: 12,
			MonsterHexes:
			[
				MonsterHexKind.FrostWraith,
				null,
				MonsterHexKind.PandorasBox
			],
			RerollCounts: [ 2, -3 ],
			IsFinal: true);

		PlayerChoiceResult result = HextechChoiceCodec.CreateEnemyHexAdjustment(OperationToken, source);

		Expect(HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 0, 12, out EnemyHexAdjustmentPayload decoded), "enemy adjustment should decode");
		Equal(0, decoded.ActIndex, "act");
		Equal(12, decoded.Sequence, "sequence");
		Equal(true, decoded.IsFinal, "final flag");
		SequenceEqual(source.MonsterHexes, decoded.MonsterHexes, "monster hex slots");
		SequenceEqual(new[] { 2, 0, 0 }, decoded.RerollCounts, "reroll counts");
		Expect(!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 1, 12, out _), "wrong act should be rejected");
	}

	[HextechTest]
	private static void EnemyHexAdjustmentRejectsInvalidHex()
	{
		const int OperationToken = 223344;
		PlayerChoiceResult result = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindEnemyHexAdjustment,
			0,
			1,
			OperationToken,
			EnemyHexAdjustmentListVersion,
			0,
			1,
			int.MaxValue,
			0
		});

		Expect(!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 0, 1, out _), "invalid monster hex enum should be rejected");
	}

	[HextechTest]
	private static void EnemyHexAdjustmentRejectsUnexpectedSequence()
	{
		const int OperationToken = 334455;
		EnemyHexAdjustmentPayload source = new(
			ActIndex: 1,
			Sequence: 3,
			MonsterHexes: [ MonsterHexKind.FrostWraith ],
			RerollCounts: [ 0 ],
			IsFinal: false);
		PlayerChoiceResult result = HextechChoiceCodec.CreateEnemyHexAdjustment(OperationToken, source);

		Expect(
			HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 1, 3, out _),
			"exact enemy adjustment sequence should decode");
		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 1, 2, out _),
			"stale enemy adjustment sequence should be rejected");
		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, OperationToken, 1, 4, out _),
			"future enemy adjustment sequence should be rejected");
	}

	[HextechTest]
	private static void EnemyHexAdjustmentRejectsExtremeCounts()
	{
		const int OperationToken = 445566;
		PlayerChoiceResult extremeHexCount = PlayerChoiceResult.FromIndexes(
		[
			Magic,
			ChoiceKindEnemyHexAdjustment,
			0,
			0,
			OperationToken,
			EnemyHexAdjustmentListVersion,
			0,
			int.MaxValue
		]);
		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(extremeHexCount, OperationToken, 0, 0, out _),
			"extreme enemy hex count should be rejected without allocation");

		PlayerChoiceResult extremeRerollCount = PlayerChoiceResult.FromIndexes(
		[
			Magic,
			ChoiceKindEnemyHexAdjustment,
			0,
			0,
			OperationToken,
			EnemyHexAdjustmentListVersion,
			0,
			0,
			int.MaxValue
		]);
		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(extremeRerollCount, OperationToken, 0, 0, out _),
			"extreme enemy reroll count should be rejected without allocation");
	}

	[HextechTest]
	private static void LegacyEnemyHexAdjustmentIsRejected()
	{
		PlayerChoiceResult result = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindEnemyHexAdjustment,
			1,
			9,
			0,
			(int)MonsterHexKind.FrostWraith,
			2,
			1
		});

		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, 556677, 1, 9, out _),
			"legacy enemy adjustment payload should be rejected after the protocol gate");
	}

	[HextechTest]
	private static void RandomRuneGrantRoundTripKeepsStableModelIds()
	{
		const int OperationToken = 667788;
		ModelId[] source =
		[
			new("HEXTECH_TEST", "FIRST_RUNE"),
			new("HEXTECH_TEST", "SECOND_RUNE")
		];

		PlayerChoiceResult result = HextechChoiceCodec.CreateRandomRuneGrant(OperationToken, source);

		Expect(HextechChoiceCodec.TryDecodeRandomRuneGrant(result, OperationToken, out List<ModelId> decoded), "random grant should decode");
		SequenceEqual(source, decoded, "stable model ids");
		Expect(HextechChoiceCodec.IsRandomRuneGrant(result, OperationToken), "random grant predicate");
	}

	[HextechTest]
	private static void RandomRuneGrantRejectsMalformedStableModelIdList()
	{
		const int OperationToken = 778899;
		PlayerChoiceResult tooManyIds = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindRandomRuneGrant,
			OperationToken,
			StableModelIdListVersion,
			65
		});

		Expect(!HextechChoiceCodec.TryDecodeRandomRuneGrant(tooManyIds, OperationToken, out _), "oversized stable id list should be rejected");

		PlayerChoiceResult badSerializedId = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindRandomRuneGrant,
			OperationToken,
			StableModelIdListVersion,
			1,
			3,
			'B',
			'A',
			'D'
		});

		Expect(!HextechChoiceCodec.TryDecodeRandomRuneGrant(badSerializedId, OperationToken, out _), "malformed model id should be rejected");

		PlayerChoiceResult runeSelectionWithOutOfRangeLegacyOrdinal = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindRuneSelection,
			1,
			2,
			0,
			0,
			1,
			int.MaxValue
		});
		Expect(
			!HextechChoiceCodec.TryDecodeRuneSelection(runeSelectionWithOutOfRangeLegacyOrdinal, 1, 2, out _),
			"out-of-range legacy rune selection ordinal should be rejected");

		PlayerChoiceResult forgeSelectionWithOutOfRangeLegacyOrdinal = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindForgeSelection,
			OperationToken,
			0,
			1,
			int.MaxValue
		});
		Expect(
			!HextechChoiceCodec.TryDecodeRelicChoice(HextechRelicChoiceKind.Forge, forgeSelectionWithOutOfRangeLegacyOrdinal, OperationToken, out _, out _),
			"out-of-range legacy forge selection ordinal should be rejected");

		PlayerChoiceResult malformedRelicOptionSelection = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindRelicOptionSelection,
			OperationToken,
			0,
			StableModelIdListVersion
		});
		Expect(
			!HextechChoiceCodec.TryDecodeRelicChoice(HextechRelicChoiceKind.RelicOption, malformedRelicOptionSelection, OperationToken, out _, out _),
			"truncated relic option selection should be rejected");

		PlayerChoiceResult randomGrantWithOutOfRangeLegacyOrdinal = PlayerChoiceResult.FromIndexes(new List<int>
		{
			Magic,
			ChoiceKindRandomRuneGrant,
			OperationToken,
			1,
			int.MaxValue
		});
		Expect(
			!HextechChoiceCodec.TryDecodeRandomRuneGrant(randomGrantWithOutOfRangeLegacyOrdinal, OperationToken, out _),
			"out-of-range legacy random grant ordinal should be rejected");
	}

	[HextechTest]
	private static void GeneratedRuneSelectionPreservesInstanceDataAndRejectsTruncation()
	{
		RelicModel[] options = [new GeneratedTestRelic { Data = "recipe:first" }, new GeneratedTestRelic { Data = "recipe:second" }];
		Expect(!HextechSelectionHelpers.SameRuneCandidate(options[0], options[1]), "same carrier with new recipe is a real reroll");
		Expect(HextechSelectionHelpers.SameRuneCandidate(options[0], new GeneratedTestRelic { Data = "recipe:first" }), "same recipe reconstruction is unchanged");
		PlayerChoiceResult choice = HextechChoiceCodec.CreateRuneSelection(1, 2, 1, [0, 1], options);
		RelicModel[] restored = [new GeneratedTestRelic(), new GeneratedTestRelic()];
		Expect(HextechChoiceCodec.TryDecodeRuneSelection(choice, 1, 2, out RuneSelectionPayload? decoded), "generated selection decodes");
		Expect(HextechGeneratedRuneDataCodec.Restore(decoded.GeneratedRuneData, restored), "generated data restores");
		Equal("recipe:second", ((GeneratedTestRelic)restored[1]).Data, "same model ID keeps separate recipes");
		List<int> truncated = choice.AsIndexes().ToList();
		truncated.RemoveAt(truncated.Count - 1);
		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(PlayerChoiceResult.FromIndexes(truncated), 1, 2, out _), "truncated recipe must fail");
		List<int> missing = [];
		HextechStableModelIdListCodec.Append(missing, options.Select(static option => option.Id));
		PlayerChoiceResult old = PlayerChoiceResult.FromIndexes(new[] { Magic, 2, 1, 2, 1, 0 }.Concat(missing).ToList());
		Expect(!HextechChoiceCodec.TryDecodeRuneSelection(old, 1, 2, out _), "payload without seen history and recipes is rejected");
		Expect(!HextechGeneratedRuneDataCodec.Restore([], restored), "missing generated data must not reroll");
		Expect(
			HextechChoiceCodec.TryDecodeRuneSelection(HextechChoiceCodec.CreateRuneSelection(1, 2, 0, [], CreateRuneSelectionTestOptions(2)), 1, 2, out RuneSelectionPayload? ordinary)
				&& HextechGeneratedRuneDataCodec.Restore(ordinary.GeneratedRuneData, CreateRuneSelectionTestOptions(2)),
			"ordinary choices keep working");

		HextechRuneSelectionJournalState journal = new();
		journal.RecordSelected(1, 2, 7, options[1].Id, "recipe:second");
		HextechRuneSelectionJournalState recovered = new();
		recovered.Restore(journal.Serialize());
		Expect(recovered.TryGet(1, 2, 7, out HextechRuneSelectionJournalEntry entry), "journal entry restored");
		Equal("recipe:second", entry.SelectionData, "pending checkpoint keeps instance data");
		Expect(!entry.Applied, "pending recipe still needs obtain");
	}

	[HextechTest]
	private static void OperationTokensRejectCrossedPayloads()
	{
		const uint ChoiceId = 42;
		const ulong PlayerNetId = 9001;
		int forgeToken = HextechChoiceCodec.ComputeOperationToken(
			"forge-selection",
			ChoiceId,
			PlayerNetId,
			"source:0");
		int sameForgeToken = HextechChoiceCodec.ComputeOperationToken(
			"forge-selection",
			ChoiceId,
			PlayerNetId,
			"source:0");
		int crossedForgeToken = HextechChoiceCodec.ComputeOperationToken(
			"forge-selection",
			ChoiceId,
			PlayerNetId,
			"source:1");
		Equal(forgeToken, sameForgeToken, "operation token must be stable");
		Expect(forgeToken != crossedForgeToken, "different stable contexts should produce different operation tokens");

		RelicModel[] options = CreateRuneSelectionTestOptions(2);
		PlayerChoiceResult forge = HextechChoiceCodec.CreateRelicChoice(HextechRelicChoiceKind.Forge, forgeToken, 0, options);
		Expect(HextechChoiceCodec.TryDecodeRelicChoice(HextechRelicChoiceKind.Forge, forge, forgeToken, out _, out _), "matching forge operation should decode");
		Expect(!HextechChoiceCodec.TryDecodeRelicChoice(HextechRelicChoiceKind.Forge, forge, crossedForgeToken, out _, out _), "crossed forge operation should be rejected");

		int relicToken = HextechChoiceCodec.ComputeOperationToken(
			"relic-option-selection",
			ChoiceId,
			PlayerNetId,
			"relic-source");
		PlayerChoiceResult relic = HextechChoiceCodec.CreateRelicChoice(HextechRelicChoiceKind.RelicOption, relicToken, 1, options);
		Expect(!HextechChoiceCodec.TryDecodeRelicChoice(HextechRelicChoiceKind.RelicOption, relic, relicToken + 1, out _, out _), "crossed relic operation should be rejected");

		int randomToken = HextechChoiceCodec.ComputeOperationToken(
			"random-rune-grant",
			ChoiceId,
			PlayerNetId,
			"consume:HEXTECH_TEST:RUNE");
		PlayerChoiceResult random = HextechChoiceCodec.CreateRandomRuneGrant(
			randomToken,
			[ new ModelId("HEXTECH_TEST", "RUNE") ]);
		Expect(!HextechChoiceCodec.TryDecodeRandomRuneGrant(random, randomToken + 1, out _), "crossed random grant operation should be rejected");

		int enemyToken = HextechChoiceCodec.ComputeOperationToken(
			"enemy-hex-adjustment",
			ChoiceId,
			PlayerNetId,
			"act=1;sequence=2");
		EnemyHexAdjustmentPayload enemyPayload = new(
			ActIndex: 1,
			Sequence: 2,
			MonsterHexes: [ MonsterHexKind.FrostWraith ],
			RerollCounts: [ 0 ],
			IsFinal: true);
		PlayerChoiceResult enemy = HextechChoiceCodec.CreateEnemyHexAdjustment(enemyToken, enemyPayload);
		Expect(
			!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(enemy, enemyToken + 1, 1, 2, out _),
			"crossed enemy adjustment operation should be rejected");
	}

	[HextechTest]
	private static void StableModelIdListCodecRoundTripsFromNonzeroCursor()
	{
		ModelId[] source =
		[
			new("HEXTECH_TEST", "FIRST"),
			new("HEXTECH_TEST", "SECOND")
		];
		List<int> payload = [ 17, 23 ];

		HextechStableModelIdListCodec.Append(payload, source);

		Expect(HextechStableModelIdListCodec.TryDecode(payload, 2, out List<ModelId> decoded, out int nextCursor), "stable model id list should decode");
		SequenceEqual(source, decoded, "stable model id helper round-trip");
		Equal(payload.Count, nextCursor, "stable model id helper next cursor");
	}

	[HextechTest]
	private static void StableModelIdListCodecRejectsMalformedLength()
	{
		List<int> payload =
		[
			HextechStableModelIdListCodec.Version,
			1,
			129
		];

		Expect(!HextechStableModelIdListCodec.TryDecode(payload, 0, out List<ModelId> decoded, out int nextCursor), "oversized stable model id length should be rejected");
		Expect(decoded.Count == 0, "malformed stable model id list should not keep partial ids");
		Equal(0, nextCursor, "failed stable model id decode should keep original cursor");
	}

	[HextechTest]
	private static void StableModelIdListCodecRejectsEncoderOverflow()
	{
		ModelId id = new("HEXTECH_TEST", "ENTRY");
		ExpectThrows<ArgumentOutOfRangeException>(
			() => HextechStableModelIdListCodec.Append(
				[],
				Enumerable.Repeat(id, HextechStableModelIdListCodec.MaxCount + 1)),
			"stable ModelId encoder should reject more than 64 items");

		ModelId oversized = new(
			new string('C', 64),
			new string('E', HextechStableModelIdListCodec.MaxSerializedLength));
		ExpectThrows<ArgumentException>(
			() => HextechStableModelIdListCodec.Append([], [ oversized ]),
			"stable ModelId encoder should reject a serialized ID longer than 128 characters");

		Expect(
			!HextechStableModelIdListCodec.TryDecode(
				[ StableModelIdListVersion, 0 ],
				-1,
				out _,
				out _),
			"stable ModelId decoder should reject a negative cursor");
	}
}

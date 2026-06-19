using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal readonly record struct EnemyHexAdjustmentPayload(
	int ActIndex,
	int Sequence,
	IReadOnlyList<MonsterHexKind?> MonsterHexes,
	IReadOnlyList<int> RerollCounts,
	bool IsFinal);

internal static class HextechChoiceCodec
{
	private const int Magic = 0x48585452; // HXTR
	private const int ChoiceKindActRoll = 1;
	private const int ChoiceKindRuneSelection = 2;
	private const int ChoiceKindActSelectionApplied = 3;
	private const int ChoiceKindEnemyHexAdjustment = 4;
	private const int ChoiceKindForgeSelection = 5;
	private const int ChoiceKindRandomRuneGrant = 6;
	private const int EnemyHexAdjustmentListVersion = -2;
	private const int PlayerRuneConfigBitsetVersion = -4;
	private const int PlayerRuneConfigBitsPerWord = 30;
	private const int MaxPlayerRuneConfigBitsetWords = 64;

	public static PlayerChoiceResult CreateActRoll(
		int actIndex,
		HextechRarityTier rarity,
		MonsterHexKind? monsterHex,
		bool hostUsesBetterMultiplayerScaling,
		IReadOnlyList<int> enemyHexCountsByAct,
		IReadOnlySet<string> disabledPlayerRuneIds)
	{
		List<int> payload =
		[
			Magic,
			ChoiceKindActRoll,
			actIndex,
			(int)rarity,
			monsterHex.HasValue ? (int)monsterHex.Value : -1,
			hostUsesBetterMultiplayerScaling ? 1 : 0
		];
		int[] normalizedCounts = HextechEnemyHexCountState.Normalize(enemyHexCountsByAct);
		payload.AddRange(normalizedCounts);
		AppendDisabledPlayerRuneConfig(payload, disabledPlayerRuneIds);
		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool TryDecodeActRoll(
		PlayerChoiceResult result,
		int expectedActIndex,
		out HextechRarityTier rarity,
		out MonsterHexKind? monsterHex,
		out bool hostUsesBetterMultiplayerScaling,
		out int[] enemyHexCountsByAct,
		out HashSet<string> disabledPlayerRuneIds)
	{
		rarity = default;
		monsterHex = null;
		hostUsesBetterMultiplayerScaling = false;
		enemyHexCountsByAct = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();
		disabledPlayerRuneIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 5
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindActRoll
			|| payload[2] != expectedActIndex)
		{
			return false;
		}

		if (!Enum.IsDefined(typeof(HextechRarityTier), payload[3]))
		{
			return false;
		}

		if (payload[4] >= 0)
		{
			if (!Enum.IsDefined(typeof(MonsterHexKind), payload[4]))
			{
				return false;
			}

			monsterHex = (MonsterHexKind)payload[4];
		}

		rarity = (HextechRarityTier)payload[3];
		hostUsesBetterMultiplayerScaling = payload.Count >= 6 && payload[5] != 0;
		if (payload.Count >= 9)
		{
			enemyHexCountsByAct = HextechEnemyHexCountState.Normalize(payload.Skip(6).Take(3).ToArray());
			return TryDecodeDisabledPlayerRuneConfig(payload, 9, out disabledPlayerRuneIds);
		}

		return true;
	}

	private static void AppendDisabledPlayerRuneConfig(List<int> payload, IReadOnlySet<string> disabledPlayerRuneIds)
	{
		IReadOnlyList<ModelId> ids = PlayerRuneIdsByOrdinal.Value;
		int wordCount = (ids.Count + PlayerRuneConfigBitsPerWord - 1) / PlayerRuneConfigBitsPerWord;
		int[] words = new int[wordCount];
		for (int i = 0; i < ids.Count; i++)
		{
			if (!disabledPlayerRuneIds.Contains(ids[i].Entry))
			{
				continue;
			}

			words[i / PlayerRuneConfigBitsPerWord] |= 1 << (i % PlayerRuneConfigBitsPerWord);
		}

		payload.Add(PlayerRuneConfigBitsetVersion);
		payload.Add(wordCount);
		payload.AddRange(words);
	}

	private static bool TryDecodeDisabledPlayerRuneConfig(List<int> payload, int cursor, out HashSet<string> disabledPlayerRuneIds)
	{
		disabledPlayerRuneIds = [];
		if (payload.Count <= cursor)
		{
			return true;
		}

		if (payload[cursor] != PlayerRuneConfigBitsetVersion)
		{
			return true;
		}

		cursor++;
		if (payload.Count <= cursor)
		{
			return false;
		}

		int wordCount = payload[cursor++];
		if (wordCount < 0 || wordCount > MaxPlayerRuneConfigBitsetWords || payload.Count < cursor + wordCount)
		{
			return false;
		}

		IReadOnlyList<ModelId> ids = PlayerRuneIdsByOrdinal.Value;
		for (int i = 0; i < ids.Count; i++)
		{
			int wordIndex = i / PlayerRuneConfigBitsPerWord;
			int bitIndex = i % PlayerRuneConfigBitsPerWord;
			if (wordIndex < wordCount && (payload[cursor + wordIndex] & (1 << bitIndex)) != 0)
			{
				disabledPlayerRuneIds.Add(ids[i].Entry);
			}
		}

		return true;
	}

	private static readonly Lazy<IReadOnlyList<ModelId>> PlayerRuneIdsByOrdinal = new(
		() => HextechCatalog.GetConfigurablePlayerRuneIds()
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray());

	public static PlayerChoiceResult CreateRuneSelection(int selectedIndex, IReadOnlyList<int> rerollHistory, IReadOnlyList<RelicModel> finalOptions)
	{
		List<int> payload = [ Magic, ChoiceKindRuneSelection, selectedIndex, rerollHistory.Count ];
		payload.AddRange(rerollHistory);
		HextechStableModelIdListCodec.Append(payload, finalOptions.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id));

		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool IsRuneSelection(PlayerChoiceResult result)
	{
		return TryDecodeRuneSelection(result, out _, out _, out _);
	}

	public static bool TryDecodeRuneSelection(PlayerChoiceResult result, out int selectedIndex, out List<int> rerollHistory)
	{
		return TryDecodeRuneSelection(result, out selectedIndex, out rerollHistory, out _);
	}

	public static bool TryDecodeRuneSelection(PlayerChoiceResult result, out int selectedIndex, out List<int> rerollHistory, out List<ModelId> finalOptionIds)
	{
		selectedIndex = -1;
		rerollHistory = [];
		finalOptionIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 4
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindRuneSelection)
		{
			return false;
		}

		selectedIndex = payload[2];
		int rerollCount = Math.Max(0, payload[3]);
		if (payload.Count < rerollCount + 4)
		{
			return false;
		}

		rerollHistory = payload.Skip(4).Take(rerollCount).ToList();
		int cursor = rerollCount + 4;
		if (payload.Count <= cursor)
		{
			return true;
		}

		if (payload[cursor] == HextechStableModelIdListCodec.Version)
		{
			return HextechStableModelIdListCodec.TryDecode(payload, cursor, out finalOptionIds, out _);
		}

		int optionCount = Math.Max(0, payload[cursor]);
		cursor++;
		if (payload.Count < cursor + optionCount)
		{
			return false;
		}

		for (int i = 0; i < optionCount; i++)
		{
			if (!TryGetRuneIdForOrdinal(payload[cursor + i], out ModelId id))
			{
				finalOptionIds.Clear();
				return true;
			}

			finalOptionIds.Add(id);
		}

		return true;
	}

	private static bool TryGetRuneIdForOrdinal(int ordinal, out ModelId id)
	{
		IReadOnlyList<ModelId> ids = PlayerRuneIdsByOrdinal.Value;
		if (ordinal < 0 || ordinal >= ids.Count)
		{
			id = null!;
			return false;
		}

		id = ids[ordinal];
		return true;
	}

	public static PlayerChoiceResult CreateRandomRuneGrant(IReadOnlyList<ModelId> runeIds)
	{
		List<int> payload = [ Magic, ChoiceKindRandomRuneGrant ];
		HextechStableModelIdListCodec.Append(payload, runeIds);
		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool IsRandomRuneGrant(PlayerChoiceResult result)
	{
		return TryDecodeRandomRuneGrant(result, out _);
	}

	public static bool TryDecodeRandomRuneGrant(PlayerChoiceResult result, out List<ModelId> runeIds)
	{
		runeIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 3
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindRandomRuneGrant)
		{
			return false;
		}

		if (payload[2] == HextechStableModelIdListCodec.Version)
		{
			return HextechStableModelIdListCodec.TryDecode(payload, 2, out runeIds, out _);
		}

		int count = Math.Max(0, payload[2]);
		if (payload.Count < 3 + count)
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			if (!TryGetRuneIdForOrdinal(payload[3 + i], out ModelId id))
			{
				runeIds.Clear();
				return false;
			}

			runeIds.Add(id);
		}

		return true;
	}

	private static readonly Lazy<IReadOnlyList<ModelId>> ForgeIdsByOrdinal = new(
		() => HextechCatalog.GetAllForgeTypes()
			.Select(ModelDb.GetId)
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray());

	public static PlayerChoiceResult CreateForgeSelection(int selectedIndex, IReadOnlyList<RelicModel> options)
	{
		List<int> payload = [ Magic, ChoiceKindForgeSelection, selectedIndex ];
		HextechStableModelIdListCodec.Append(payload, options.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id));

		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool IsForgeSelection(PlayerChoiceResult result)
	{
		return TryDecodeForgeSelection(result, out _, out _);
	}

	public static bool TryDecodeForgeSelection(PlayerChoiceResult result, out int selectedIndex, out List<ModelId> optionIds)
	{
		selectedIndex = -1;
		optionIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 3
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindForgeSelection)
		{
			return false;
		}

		selectedIndex = payload[2];
		if (payload.Count <= 3)
		{
			return true;
		}

		if (payload[3] == HextechStableModelIdListCodec.Version)
		{
			return HextechStableModelIdListCodec.TryDecode(payload, 3, out optionIds, out _);
		}

		int optionCount = Math.Max(0, payload[3]);
		if (payload.Count < 4 + optionCount)
		{
			return false;
		}

		for (int i = 0; i < optionCount; i++)
		{
			if (!TryGetForgeIdForOrdinal(payload[4 + i], out ModelId id))
			{
				optionIds.Clear();
				return true;
			}

			optionIds.Add(id);
		}

		return true;
	}

	private static bool TryGetForgeIdForOrdinal(int ordinal, out ModelId id)
	{
		IReadOnlyList<ModelId> ids = ForgeIdsByOrdinal.Value;
		if (ordinal < 0 || ordinal >= ids.Count)
		{
			id = null!;
			return false;
		}

		id = ids[ordinal];
		return true;
	}

	public static PlayerChoiceResult CreateActSelectionApplied(int actIndex)
	{
		return PlayerChoiceResult.FromIndexes([ Magic, ChoiceKindActSelectionApplied, actIndex, 1 ]);
	}

	public static bool TryDecodeActSelectionApplied(PlayerChoiceResult result, int expectedActIndex)
	{
		return TryGetIndexPayload(result, out List<int> payload)
			&& payload.Count >= 4
			&& payload[0] == Magic
			&& payload[1] == ChoiceKindActSelectionApplied
			&& payload[2] == expectedActIndex
			&& payload[3] == 1;
	}

	public static PlayerChoiceResult CreateEnemyHexAdjustment(EnemyHexAdjustmentPayload payload)
	{
		List<int> indexes =
		[
			Magic,
			ChoiceKindEnemyHexAdjustment,
			payload.ActIndex,
			payload.Sequence,
			EnemyHexAdjustmentListVersion,
			payload.IsFinal ? 1 : 0,
			payload.MonsterHexes.Count
		];
		indexes.AddRange(payload.MonsterHexes.Select(static hex => hex.HasValue ? (int)hex.Value : -1));
		indexes.Add(payload.RerollCounts.Count);
		indexes.AddRange(payload.RerollCounts.Select(static count => Math.Max(0, count)));
		return PlayerChoiceResult.FromIndexes(indexes);
	}

	public static bool TryDecodeEnemyHexAdjustment(PlayerChoiceResult result, int expectedActIndex, out EnemyHexAdjustmentPayload payload)
	{
		payload = default;
		if (!TryGetIndexPayload(result, out List<int> indexes)
			|| indexes.Count < 6
			|| indexes[0] != Magic
			|| indexes[1] != ChoiceKindEnemyHexAdjustment
			|| indexes[2] != expectedActIndex)
		{
			return false;
		}

		if (indexes.Count < 7 || indexes[4] != EnemyHexAdjustmentListVersion)
		{
			return indexes.Count >= 8 && TryDecodeLegacyEnemyHexAdjustment(indexes, out payload);
		}

		bool isFinal = indexes[5] != 0;
		int hexCount = Math.Max(0, indexes[6]);
		int cursor = 7;
		if (indexes.Count < cursor + hexCount + 1)
		{
			return false;
		}

		List<MonsterHexKind?> monsterHexes = new(hexCount);
		for (int i = 0; i < hexCount; i++)
		{
			int rawHex = indexes[cursor + i];
			if (rawHex < 0)
			{
				monsterHexes.Add(null);
				continue;
			}

			if (!Enum.IsDefined(typeof(MonsterHexKind), rawHex))
			{
				return false;
			}

			monsterHexes.Add((MonsterHexKind)rawHex);
		}

		cursor += hexCount;
		int rerollCount = Math.Max(0, indexes[cursor]);
		cursor++;
		if (indexes.Count < cursor + rerollCount)
		{
			return false;
		}

		List<int> rerollCounts = indexes.Skip(cursor).Take(rerollCount).Select(static count => Math.Max(0, count)).ToList();
		while (rerollCounts.Count < monsterHexes.Count)
		{
			rerollCounts.Add(0);
		}

		payload = new EnemyHexAdjustmentPayload(
			indexes[2],
			Math.Max(0, indexes[3]),
			monsterHexes,
			rerollCounts,
			isFinal);
		return true;
	}

	private static bool TryDecodeLegacyEnemyHexAdjustment(IReadOnlyList<int> indexes, out EnemyHexAdjustmentPayload payload)
	{
		payload = default;
		MonsterHexKind? monsterHex = null;
		if (indexes[5] >= 0)
		{
			if (!Enum.IsDefined(typeof(MonsterHexKind), indexes[5]))
			{
				return false;
			}

			monsterHex = (MonsterHexKind)indexes[5];
		}

		payload = new EnemyHexAdjustmentPayload(
			indexes[2],
			Math.Max(0, indexes[3]),
			[ indexes[4] != 0 ? null : monsterHex ],
			[ Math.Max(0, indexes[6]) ],
			indexes[7] != 0);
		return true;
	}

	public static bool TryGetIndexPayload(PlayerChoiceResult result, out List<int> payload)
	{
		payload = [];
		try
		{
			List<int>? indexes = result.AsIndexes();
			if (indexes == null)
			{
				return false;
			}

			payload = indexes;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}
}

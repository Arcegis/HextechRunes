using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal readonly record struct EnemyHexAdjustmentPayload(
	int ActIndex,
	int Sequence,
	MonsterHexKind? MonsterHex,
	bool Removed,
	int RerollCount,
	bool IsFinal);

internal static class HextechChoiceCodec
{
	private const int Magic = 0x48585452; // HXTR
	private const int ChoiceKindActRoll = 1;
	private const int ChoiceKindRuneSelection = 2;
	private const int ChoiceKindActSelectionApplied = 3;
	private const int ChoiceKindEnemyHexAdjustment = 4;
	private const int ChoiceKindForgeSelection = 5;

	public static PlayerChoiceResult CreateActRoll(int actIndex, HextechRarityTier rarity, MonsterHexKind monsterHex, bool hostUsesBetterMultiplayerScaling)
	{
		return PlayerChoiceResult.FromIndexes([ Magic, ChoiceKindActRoll, actIndex, (int)rarity, (int)monsterHex, hostUsesBetterMultiplayerScaling ? 1 : 0 ]);
	}

	public static bool TryDecodeActRoll(PlayerChoiceResult result, int expectedActIndex, out HextechRarityTier rarity, out MonsterHexKind monsterHex, out bool hostUsesBetterMultiplayerScaling)
	{
		rarity = default;
		monsterHex = default;
		hostUsesBetterMultiplayerScaling = false;
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 5
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindActRoll
			|| payload[2] != expectedActIndex)
		{
			return false;
		}

		if (!Enum.IsDefined(typeof(HextechRarityTier), payload[3]) || !Enum.IsDefined(typeof(MonsterHexKind), payload[4]))
		{
			return false;
		}

		rarity = (HextechRarityTier)payload[3];
		monsterHex = (MonsterHexKind)payload[4];
		hostUsesBetterMultiplayerScaling = payload.Count >= 6 && payload[5] != 0;
		return true;
	}

	private static readonly Lazy<IReadOnlyList<ModelId>> PlayerRuneIdsByOrdinal = new(
		() => HextechCatalog.GetConfigurablePlayerRuneIds()
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray());

	private static readonly Lazy<IReadOnlyDictionary<ModelId, int>> PlayerRuneOrdinalById = new(
		() => PlayerRuneIdsByOrdinal.Value
			.Select(static (id, index) => (id, index))
			.ToDictionary(static item => item.id, static item => item.index));

	public static PlayerChoiceResult CreateRuneSelection(int selectedIndex, IReadOnlyList<int> rerollHistory, IReadOnlyList<RelicModel> finalOptions)
	{
		List<int> payload = [ Magic, ChoiceKindRuneSelection, selectedIndex, rerollHistory.Count ];
		payload.AddRange(rerollHistory);
		if (TryEncodeRuneOptionOrdinals(finalOptions, out List<int> optionOrdinals))
		{
			payload.Add(optionOrdinals.Count);
			payload.AddRange(optionOrdinals);
		}

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

	private static bool TryEncodeRuneOptionOrdinals(IReadOnlyList<RelicModel> finalOptions, out List<int> optionOrdinals)
	{
		optionOrdinals = new(finalOptions.Count);
		foreach (RelicModel relic in finalOptions)
		{
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			if (!PlayerRuneOrdinalById.Value.TryGetValue(id, out int ordinal))
			{
				optionOrdinals.Clear();
				return false;
			}

			optionOrdinals.Add(ordinal);
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

	private static readonly Lazy<IReadOnlyList<ModelId>> ForgeIdsByOrdinal = new(
		() => HextechCatalog.GetAllForgeTypes()
			.Select(ModelDb.GetId)
			.OrderBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray());

	private static readonly Lazy<IReadOnlyDictionary<ModelId, int>> ForgeOrdinalById = new(
		() => ForgeIdsByOrdinal.Value
			.Select(static (id, index) => (id, index))
			.ToDictionary(static item => item.id, static item => item.index));

	public static PlayerChoiceResult CreateForgeSelection(int selectedIndex, IReadOnlyList<RelicModel> options)
	{
		List<int> payload = [ Magic, ChoiceKindForgeSelection, selectedIndex ];
		if (TryEncodeForgeOptionOrdinals(options, out List<int> optionOrdinals))
		{
			payload.Add(optionOrdinals.Count);
			payload.AddRange(optionOrdinals);
		}

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

	private static bool TryEncodeForgeOptionOrdinals(IReadOnlyList<RelicModel> options, out List<int> optionOrdinals)
	{
		optionOrdinals = new(options.Count);
		foreach (RelicModel relic in options)
		{
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			if (!ForgeOrdinalById.Value.TryGetValue(id, out int ordinal))
			{
				optionOrdinals.Clear();
				return false;
			}

			optionOrdinals.Add(ordinal);
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
		return PlayerChoiceResult.FromIndexes(
		[
			Magic,
			ChoiceKindEnemyHexAdjustment,
			payload.ActIndex,
			payload.Sequence,
			payload.Removed ? 1 : 0,
			payload.MonsterHex.HasValue ? (int)payload.MonsterHex.Value : -1,
			payload.RerollCount,
			payload.IsFinal ? 1 : 0
		]);
	}

	public static bool TryDecodeEnemyHexAdjustment(PlayerChoiceResult result, int expectedActIndex, out EnemyHexAdjustmentPayload payload)
	{
		payload = default;
		if (!TryGetIndexPayload(result, out List<int> indexes)
			|| indexes.Count < 8
			|| indexes[0] != Magic
			|| indexes[1] != ChoiceKindEnemyHexAdjustment
			|| indexes[2] != expectedActIndex)
		{
			return false;
		}

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
			monsterHex,
			indexes[4] != 0,
			Math.Max(0, indexes[6]),
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

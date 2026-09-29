using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.GameActions;

namespace HextechRunes;

internal static partial class HextechChoiceCodec
{
	// [Magic, kind, act, ordinal, selectedIndex, rerollCount] + rerollHistory + 最终候选 ID 列表 + 本次展示过的候选历史 + 可选尾部(权重、实例配方)。
	private const int RuneSelectionHeaderCount = 6;
	private const int RuneSelectionRerollCountIndex = 5;

	private static readonly object PlayerRuneIdsByOrdinalLock = new();
	private static IReadOnlyList<ModelId>? _playerRuneIdsByOrdinal;
	private static int _playerRuneIdsByOrdinalVersion = -1;

	// 联机位图/旧格式序号按“可配置玩家符文 ID 排序后的下标”编码。外部模组登记会改变该集合,
	// 所以缓存按 HextechContentRegistry.Version 失效:否则在外部登记完成前被访问一次就会永久错位。
	private static IReadOnlyList<ModelId> PlayerRuneIdsByOrdinal
	{
		get
		{
			int version = HextechContentRegistry.Version;
			lock (PlayerRuneIdsByOrdinalLock)
			{
				if (_playerRuneIdsByOrdinal == null || _playerRuneIdsByOrdinalVersion != version)
				{
					_playerRuneIdsByOrdinal = HextechCatalog.GetConfigurablePlayerRuneIds()
						.OrderBy(static id => id.Category, StringComparer.Ordinal)
						.ThenBy(static id => id.Entry, StringComparer.Ordinal)
						.ToArray();
					_playerRuneIdsByOrdinalVersion = version;
				}

				return _playerRuneIdsByOrdinal;
			}
		}
	}

	public static PlayerChoiceResult CreateRuneSelection(int actIndex, int choiceOrdinal, int selectedIndex, IReadOnlyList<int> rerollHistory, IReadOnlyList<RelicModel> finalOptions, IEnumerable<ModelId>? seenOptionIds = null)
	{
		List<int> payload = [ Magic, ChoiceKindRuneSelection, actIndex, choiceOrdinal, selectedIndex, rerollHistory.Count ];
		payload.AddRange(rerollHistory);
		ModelId[] finalIds = finalOptions.Select(static relic => relic.CanonicalId()).ToArray();
		HextechStableModelIdListCodec.Append(payload, finalIds);
		HextechRuneSeenHistoryCodec.Append(payload, (seenOptionIds ?? []).Concat(finalIds));
		HextechRuneWeightCodec.Append(payload, finalOptions);
		HextechGeneratedRuneDataCodec.Append(payload, finalOptions);

		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool IsRuneSelection(PlayerChoiceResult result)
	{
		return TryGetIndexPayload(result, out List<int> payload)
			&& payload.Count >= 2
			&& payload[0] == Magic
			&& payload[1] == ChoiceKindRuneSelection;
	}

	public static bool IsRuneSelection(PlayerChoiceResult result, int expectedActIndex, int expectedChoiceOrdinal)
	{
		return TryDecodeRuneSelection(result, expectedActIndex, expectedChoiceOrdinal, out _, out _, out _);
	}

	public static bool TryDecodeRuneSelection(
		PlayerChoiceResult result,
		int expectedActIndex,
		int expectedChoiceOrdinal,
		out int selectedIndex,
		out List<int> rerollHistory,
		out List<ModelId> finalOptionIds)
	{
		return TryDecodeRuneSelection(result, expectedActIndex, expectedChoiceOrdinal,
			out selectedIndex, out rerollHistory, out finalOptionIds, out _);
	}

	public static bool TryDecodeRuneSelection(
		PlayerChoiceResult result,
		int expectedActIndex,
		int expectedChoiceOrdinal,
		out int selectedIndex,
		out List<int> rerollHistory,
		out List<ModelId> finalOptionIds,
		out List<ModelId> seenOptionIds)
	{
		selectedIndex = -1;
		rerollHistory = [];
		finalOptionIds = [];
		seenOptionIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| !TryReadRuneSelectionHeader(payload, out int finalOptionsCursor)
			|| payload[2] != expectedActIndex
			|| payload[3] != expectedChoiceOrdinal)
		{
			return false;
		}

		selectedIndex = payload[4];
		rerollHistory = payload.Skip(RuneSelectionHeaderCount).Take(payload[RuneSelectionRerollCountIndex]).ToList();
		return TryDecodeRuneSelectionFinalOptions(payload, finalOptionsCursor, out finalOptionIds, out seenOptionIds);
	}

	/// <summary>
	/// 校验符文选择载荷的头部(Magic、消息类型、重掷历史长度),返回最终候选 ID 列表的起始下标。
	/// 候选之后的已见历史/权重/配方尾部解析器用它定位,不再各自按魔法下标重解析头部。
	/// </summary>
	internal static bool TryReadRuneSelectionHeader(IReadOnlyList<int> payload, out int finalOptionsCursor)
	{
		finalOptionsCursor = -1;
		if (payload.Count < RuneSelectionHeaderCount
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindRuneSelection)
		{
			return false;
		}

		// 无限重随可超过单次候选列表的 64 项限制,历史长度由实际载荷约束。
		int rerollCount = payload[RuneSelectionRerollCountIndex];
		if (rerollCount < 0
			|| !HasRemaining(payload, RuneSelectionHeaderCount, rerollCount))
		{
			return false;
		}

		finalOptionsCursor = RuneSelectionHeaderCount + rerollCount;
		return true;
	}

	private static bool TryDecodeRuneSelectionFinalOptions(List<int> payload, int cursor, out List<ModelId> finalOptionIds, out List<ModelId> seenOptionIds)
	{
		finalOptionIds = [];
		seenOptionIds = [];
		if (payload.Count <= cursor)
		{
			return false;
		}

		if (payload[cursor] == HextechStableModelIdListCodec.Version)
		{
			return HextechStableModelIdListCodec.TryDecode(payload, cursor, out finalOptionIds, out int nextCursor)
				&& HextechRuneSeenHistoryCodec.TryRead(payload, ref nextCursor, out seenOptionIds)
				&& finalOptionIds.All(seenOptionIds.Contains)
				&& HextechRuneWeightCodec.TryRead(payload, ref nextCursor, out _)
				&& HextechGeneratedRuneDataCodec.TryDecode(payload, nextCursor, finalOptionIds.Count, out _);
		}

		int optionCount = payload[cursor];
		cursor++;
		if (optionCount < 0
			|| optionCount > MaxChoiceListCount
			|| !HasRemaining(payload, cursor, optionCount))
		{
			return false;
		}

		for (int i = 0; i < optionCount; i++)
		{
			if (!TryGetRuneIdForOrdinal(payload[cursor + i], out ModelId? id))
			{
				finalOptionIds.Clear();
				return false;
			}

			finalOptionIds.Add(id);
		}

		cursor += optionCount;
		return HextechRuneSeenHistoryCodec.TryRead(payload, ref cursor, out seenOptionIds)
			&& finalOptionIds.All(seenOptionIds.Contains)
			&& HextechRuneWeightCodec.TryRead(payload, ref cursor, out _)
			&& HextechGeneratedRuneDataCodec.TryDecode(payload, cursor, finalOptionIds.Count, out _);
	}

	private static bool TryGetRuneIdForOrdinal(int ordinal, [NotNullWhen(true)] out ModelId? id)
	{
		IReadOnlyList<ModelId> ids = PlayerRuneIdsByOrdinal;
		if (ordinal < 0 || ordinal >= ids.Count)
		{
			id = null;
			return false;
		}

		id = ids[ordinal];
		return true;
	}
}

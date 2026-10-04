using MegaCrit.Sts2.Core.GameActions;

namespace HextechRunes;

/// <summary>
/// 锻造选择与遗物选项选择共用的线格式:[Magic, 消息类型, 操作令牌, 选中下标] + 候选 ModelId 列表。
/// 两者只差消息类型编号(5 / 7),编号即联机契约,不能改。
/// </summary>
internal enum HextechRelicChoiceKind
{
	Forge = 5,
	RelicOption = 7
}

internal static partial class HextechChoiceCodec
{
	private const int RelicChoiceHeaderCount = 4;

	public static PlayerChoiceResult CreateRelicChoice(
		HextechRelicChoiceKind kind,
		int operationToken,
		int selectedIndex,
		IReadOnlyList<RelicModel> options)
	{
		List<int> payload = [ Magic, GetRelicChoiceMessageKind(kind), operationToken, selectedIndex ];
		HextechStableModelIdListCodec.Append(payload, options.Select(static relic => relic.CanonicalId()));
		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool TryDecodeRelicChoice(
		HextechRelicChoiceKind kind,
		PlayerChoiceResult result,
		int expectedOperationToken,
		out int selectedIndex,
		out List<ModelId> optionIds)
	{
		selectedIndex = -1;
		optionIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < RelicChoiceHeaderCount + 1
			|| payload[0] != Magic
			|| payload[1] != GetRelicChoiceMessageKind(kind)
			|| payload[2] != expectedOperationToken
			|| payload[RelicChoiceHeaderCount] != HextechStableModelIdListCodec.Version)
		{
			return false;
		}

		selectedIndex = payload[3];
		return HextechStableModelIdListCodec.TryDecode(payload, RelicChoiceHeaderCount, out optionIds, out _);
	}

	/// <summary>远端候选 ID 与本端候选逐个相同(顺序一致)。</summary>
	internal static bool MatchesOptionIds(IReadOnlyList<ModelId> optionIds, IReadOnlyList<RelicModel> expectedOptions)
	{
		if (optionIds.Count != expectedOptions.Count)
		{
			return false;
		}

		for (int i = 0; i < expectedOptions.Count; i++)
		{
			if (optionIds[i] != expectedOptions[i].CanonicalId())
			{
				return false;
			}
		}

		return true;
	}

	private static int GetRelicChoiceMessageKind(HextechRelicChoiceKind kind)
	{
		return kind switch
		{
			HextechRelicChoiceKind.Forge => ChoiceKindForgeSelection,
			HextechRelicChoiceKind.RelicOption => ChoiceKindRelicOptionSelection,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown relic choice kind.")
		};
	}
}

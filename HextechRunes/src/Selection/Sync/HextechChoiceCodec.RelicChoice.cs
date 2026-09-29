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

	/// <summary>载荷可解码且候选 ID 与本端候选逐个相同(顺序一致)。</summary>
	public static bool IsRelicChoice(
		HextechRelicChoiceKind kind,
		PlayerChoiceResult result,
		int expectedOperationToken,
		IReadOnlyList<RelicModel> expectedOptions)
	{
		return TryDecodeRelicChoice(kind, result, expectedOperationToken, out _, out List<ModelId> optionIds)
			&& MatchesOptionIds(optionIds, expectedOptions);
	}

	public static bool IsMalformedRelicChoiceEnvelope(HextechRelicChoiceKind kind, PlayerChoiceResult result, int expectedOperationToken)
	{
		return IsChoiceEnvelope(result, GetRelicChoiceMessageKind(kind))
			&& !TryDecodeRelicChoice(kind, result, expectedOperationToken, out _, out _);
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

	// 按消息类型命名的入口(协议测试使用),实现统一走上面的共享编解码。
	public static PlayerChoiceResult CreateForgeSelection(int operationToken, int selectedIndex, IReadOnlyList<RelicModel> options)
	{
		return CreateRelicChoice(HextechRelicChoiceKind.Forge, operationToken, selectedIndex, options);
	}

	public static bool TryDecodeForgeSelection(PlayerChoiceResult result, int expectedOperationToken, out int selectedIndex, out List<ModelId> optionIds)
	{
		return TryDecodeRelicChoice(HextechRelicChoiceKind.Forge, result, expectedOperationToken, out selectedIndex, out optionIds);
	}

	public static bool IsMalformedForgeSelectionEnvelope(PlayerChoiceResult result, int expectedOperationToken)
	{
		return IsMalformedRelicChoiceEnvelope(HextechRelicChoiceKind.Forge, result, expectedOperationToken);
	}

	public static PlayerChoiceResult CreateRelicOptionSelection(int operationToken, int selectedIndex, IReadOnlyList<RelicModel> options)
	{
		return CreateRelicChoice(HextechRelicChoiceKind.RelicOption, operationToken, selectedIndex, options);
	}

	public static bool IsRelicOptionSelection(PlayerChoiceResult result, int expectedOperationToken, IReadOnlyList<RelicModel> expectedOptions)
	{
		return IsRelicChoice(HextechRelicChoiceKind.RelicOption, result, expectedOperationToken, expectedOptions);
	}

	public static bool TryDecodeRelicOptionSelection(PlayerChoiceResult result, int expectedOperationToken, out int selectedIndex, out List<ModelId> optionIds)
	{
		return TryDecodeRelicChoice(HextechRelicChoiceKind.RelicOption, result, expectedOperationToken, out selectedIndex, out optionIds);
	}

	public static bool IsMalformedRelicOptionSelectionEnvelope(PlayerChoiceResult result, int expectedOperationToken)
	{
		return IsMalformedRelicChoiceEnvelope(HextechRelicChoiceKind.RelicOption, result, expectedOperationToken);
	}
}

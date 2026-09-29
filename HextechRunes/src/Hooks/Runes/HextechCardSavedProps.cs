namespace HextechRunes;

/// <summary>
/// 原版卡牌存档 <see cref="SerializableCard.Props"/> 的整数项读写。本模组给原版卡附加的持久标记（关键词持久化）
/// 与累计值（自升级卡牌）都存在这里：键名与"值 1 表示有标记"是存档/联机兼容契约，不能改。
/// </summary>
internal static class HextechCardSavedProps
{
	/// <summary>移除所有同名项后在末尾写入新值。</summary>
	internal static void SetInt(SerializableCard card, string name, int value)
	{
		List<SavedProperties.SavedProperty<int>> ints = EnsureInts(card);
		ints.RemoveAll(property => property.name == name);
		ints.Add(new SavedProperties.SavedProperty<int>(name, value));
	}

	/// <summary>没有同名项时在末尾写入；已有同名项（无论值为何）保持原样。</summary>
	internal static void AddIntIfMissing(SerializableCard card, string name, int value)
	{
		List<SavedProperties.SavedProperty<int>> ints = EnsureInts(card);
		if (ints.Any(property => property.name == name))
		{
			return;
		}

		ints.Add(new SavedProperties.SavedProperty<int>(name, value));
	}

	/// <summary>第一个同名项的值；没有同名项时为 0。</summary>
	internal static int GetInt(SavedProperties? props, string name)
	{
		if (props?.ints == null)
		{
			return 0;
		}

		foreach (SavedProperties.SavedProperty<int> property in props.ints)
		{
			if (property.name == name)
			{
				return property.value;
			}
		}

		return 0;
	}

	/// <summary>存在任一同名且非 0 的项（标记语义）。</summary>
	internal static bool HasNonZeroInt(SavedProperties? props, string name)
	{
		return props?.ints?.Any(property => property.name == name && property.value != 0) == true;
	}

	private static List<SavedProperties.SavedProperty<int>> EnsureInts(SerializableCard card)
	{
		card.Props ??= new SavedProperties();
		card.Props.ints ??= new List<SavedProperties.SavedProperty<int>>();
		return card.Props.ints;
	}
}

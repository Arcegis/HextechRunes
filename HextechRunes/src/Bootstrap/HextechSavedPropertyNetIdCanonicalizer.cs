namespace HextechRunes;

/// <summary>
/// 纯逻辑:把 SavedProperties 的「net-id → 属性名」映射规范化为确定性布局。
///
/// 背景:游戏的 <c>SavedPropertiesTypeCache._netIdToPropertyNameMap</c> 是一张「先到先得、按追加顺序分配下标」的共享表,
/// net-id 就是属性名在表中的下标。原版前缀由静态构造从固定的 1624 个类型确定性种下;模组属性名由各模组的注入调用
/// 依「加载顺序 / 注入时机」追加。当存在两个以上都会注入 SavedProperties、又共用属性名的模组(如海克斯符文与
/// 其它自注入模组,叠加 RitsuLib 在 LocManager.Initialize 阶段的补注入)时,两端因本地模组加载顺序不同,会给同一个
/// 属性算出不同的 net-id —— 运行时一个 SavedProperties 包就会按错位的 net-id (反)序列化,抛 "SavedProperty net ID..."
/// 异常,被海克斯符文自身的断连兜底转成 NetError.ModMismatch(联机错误码 1014「模组不匹配」)。
///
/// 解决:把模组后缀按属性名的序数(Ordinal)排序,使整张表的布局变成「原版前缀(不动) + 模组属性名集合的序数序」,
/// 从而**只依赖加载的模组属性名集合、与注入顺序/时机/加载顺序无关**。握手已保证两端的玩法模组集合与海克斯符文版本一致,
/// 因此两端会算出**完全相同**的 net-id 布局,从根本上消除该非确定性。
///
/// 本类不依赖任何游戏类型,便于单元测试;读写真实缓存的反射外壳见 <see cref="HextechSavedPropertyNetIdHooks"/>。
/// </summary>
internal static class HextechSavedPropertyNetIdCanonicalizer
{
	/// <summary>
	/// 返回规范化后的「net-id → 属性名」有序列表;输入非法时返回 <c>null</c>(调用方应放弃改写,保持原状)。
	/// </summary>
	/// <param name="netIdToPropertyName">当前的 net-id → 属性名列表(下标即 net-id)。</param>
	/// <param name="vanillaPropertyNames">原版(非模组)拥有的 SavedProperty 属性名集合;用于把原版条目原样保留。</param>
	internal static List<string>? Canonicalize(IReadOnlyList<string>? netIdToPropertyName, IReadOnlySet<string>? vanillaPropertyNames)
	{
		if (netIdToPropertyName == null || vanillaPropertyNames == null)
		{
			return null;
		}

		List<string> vanilla = [];
		List<string> modded = [];
		foreach (string name in netIdToPropertyName)
		{
			// 原版属性名保留其原有顺序(net-id 由游戏确定性种子决定,两端一致);其余视为模组后缀。
			// 与原版同名的模组属性会被先到先得地归入原版前缀,因此天然保留原版 net-id。
			if (vanillaPropertyNames.Contains(name))
			{
				vanilla.Add(name);
			}
			else
			{
				modded.Add(name);
			}
		}

		modded.Sort(StringComparer.Ordinal);

		List<string> result = new(vanilla.Count + modded.Count);
		result.AddRange(vanilla);
		result.AddRange(modded);
		return result;
	}

	/// <summary>
	/// 与游戏 / RitsuLib 一致的 net-id 位宽公式:CeilToInt(Log2(count))。
	/// 必须与游戏完全一致,确保即便存在「部分状态」也能对齐位宽。
	/// </summary>
	internal static int ComputeNetIdBitSize(int propertyNameCount)
	{
		return propertyNameCount <= 0 ? 0 : (int)Math.Ceiling(Math.Log2(propertyNameCount));
	}
}

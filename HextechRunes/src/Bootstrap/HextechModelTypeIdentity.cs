namespace HextechRunes;

internal static class HextechModelTypeIdentity
{
	internal static IReadOnlyList<Type> Distinct(IEnumerable<Type> modelTypes)
	{
		// 下游是 SavedProperty 注入顺序，去重必须显式保序，不能依赖 Dictionary.Values 未文档化的插入序。
		List<Type> result = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		foreach (Type modelType in modelTypes)
		{
			string identity = $"{modelType.Assembly.GetName().Name}:{modelType.FullName}";
			if (seen.Add(identity))
			{
				result.Add(modelType);
			}
		}

		return result;
	}

	internal static bool IsSame(Type left, Type right)
	{
		return left == right
			|| (string.Equals(left.FullName, right.FullName, StringComparison.Ordinal)
				&& string.Equals(left.Assembly.GetName().Name, right.Assembly.GetName().Name, StringComparison.Ordinal));
	}
}

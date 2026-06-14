namespace HextechRunes;

internal static class HextechModelTypeIdentity
{
	internal static IReadOnlyList<Type> Distinct(IEnumerable<Type> modelTypes)
	{
		Dictionary<string, Type> byIdentity = new(StringComparer.Ordinal);
		foreach (Type modelType in modelTypes)
		{
			string identity = $"{modelType.Assembly.GetName().Name}:{modelType.FullName}";
			byIdentity.TryAdd(identity, modelType);
		}

		return byIdentity.Values.ToArray();
	}

	internal static bool IsSame(Type left, Type right)
	{
		return left == right
			|| (string.Equals(left.FullName, right.FullName, StringComparison.Ordinal)
				&& string.Equals(left.Assembly.GetName().Name, right.Assembly.GetName().Name, StringComparison.Ordinal));
	}
}

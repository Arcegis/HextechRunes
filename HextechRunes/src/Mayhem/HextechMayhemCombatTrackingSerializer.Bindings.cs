using System.Collections;

namespace HextechRunes;

internal static partial class HextechMayhemCombatTrackingSerializer
{
	private static IReadOnlyList<CombatTrackingFieldBinding> CreatePersistentFieldBindings()
	{
		Dictionary<string, FieldInfo> stateFields = GetPublicStateFields()
			.ToDictionary(static field => field.Name, StringComparer.Ordinal);

		CombatTrackingFieldBinding[] bindings = typeof(CombatTrackingSnapshot)
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Select(property =>
			{
				if (!stateFields.TryGetValue(property.Name, out FieldInfo? stateField))
				{
					throw new InvalidOperationException($"Combat tracking snapshot field '{property.Name}' has no matching state field.");
				}

				if (stateField.GetCustomAttribute<CombatTrackingTransientAttribute>() != null)
				{
					throw new InvalidOperationException($"Combat tracking field '{stateField.Name}' cannot be both saved and transient.");
				}

				ValidateSnapshotPropertyType(stateField, property);
				return new CombatTrackingFieldBinding(stateField, property);
			})
			.ToArray();

		ValidateStateFieldCoverage(stateFields.Values, bindings);
		return bindings;
	}

	private static IReadOnlyList<FieldInfo> CreateTransientFields()
	{
		return GetPublicStateFields()
			.Where(static field => field.GetCustomAttribute<CombatTrackingTransientAttribute>() != null)
			.ToArray();
	}

	private static FieldInfo[] GetPublicStateFields()
	{
		return typeof(HextechMayhemCombatTrackingState)
			.GetFields(BindingFlags.Instance | BindingFlags.Public);
	}

	private static void ValidateStateFieldCoverage(IEnumerable<FieldInfo> stateFields, IReadOnlyList<CombatTrackingFieldBinding> persistentBindings)
	{
		HashSet<string> persistentFieldNames = persistentBindings
			.Select(static binding => binding.StateField.Name)
			.ToHashSet(StringComparer.Ordinal);
		string[] missingFields = stateFields
			.Where(static field => field.GetCustomAttribute<CombatTrackingTransientAttribute>() == null)
			.Where(field => !persistentFieldNames.Contains(field.Name))
			.Select(static field => field.Name)
			.OrderBy(static name => name, StringComparer.Ordinal)
			.ToArray();
		if (missingFields.Length > 0)
		{
			throw new InvalidOperationException($"Combat tracking fields must be saved or marked transient: {string.Join(", ", missingFields)}.");
		}
	}

	private static void ValidateSnapshotPropertyType(FieldInfo stateField, PropertyInfo snapshotProperty)
	{
		Type stateType = stateField.FieldType;
		Type snapshotType = snapshotProperty.PropertyType;
		if (typeof(IDictionary).IsAssignableFrom(stateType))
		{
			ValidateAssignableGenericArguments(stateField, snapshotProperty, typeof(Dictionary<,>));
			return;
		}

		if (IsHashSet(stateType))
		{
			ValidateAssignableGenericArguments(stateField, snapshotProperty, typeof(List<>));
			return;
		}

		if (snapshotType != stateType)
		{
			throw new InvalidOperationException($"Combat tracking snapshot property '{snapshotProperty.Name}' has type {snapshotType}, expected {stateType}.");
		}
	}

	private static void ValidateAssignableGenericArguments(FieldInfo stateField, PropertyInfo snapshotProperty, Type expectedSnapshotGenericType)
	{
		Type stateType = stateField.FieldType;
		Type snapshotType = snapshotProperty.PropertyType;
		if (!snapshotType.IsGenericType || snapshotType.GetGenericTypeDefinition() != expectedSnapshotGenericType)
		{
			throw new InvalidOperationException($"Combat tracking snapshot property '{snapshotProperty.Name}' has type {snapshotType}, expected {expectedSnapshotGenericType}.");
		}

		Type[] stateArguments = stateType.GetGenericArguments();
		Type[] snapshotArguments = snapshotType.GetGenericArguments();
		if (!stateArguments.SequenceEqual(snapshotArguments))
		{
			throw new InvalidOperationException($"Combat tracking snapshot property '{snapshotProperty.Name}' generic arguments do not match field '{stateField.Name}'.");
		}
	}
}

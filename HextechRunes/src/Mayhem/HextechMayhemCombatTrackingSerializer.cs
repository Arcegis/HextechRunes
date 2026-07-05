using System.Text.Json;

namespace HextechRunes;

internal static partial class HextechMayhemCombatTrackingSerializer
{
	private static readonly IReadOnlyList<CombatTrackingFieldBinding> PersistentFieldBindings = CreatePersistentFieldBindings();
	private static readonly IReadOnlyList<FieldInfo> TransientFields = CreateTransientFields();

	private readonly record struct CombatTrackingFieldBinding(FieldInfo StateField, PropertyInfo SnapshotProperty);

	public static string Serialize(HextechMayhemCombatTrackingState state)
	{
		if (!HasState(state))
		{
			return "";
		}

		return JsonSerializer.Serialize(CreateSnapshot(state));
	}

	public static void Restore(HextechMayhemCombatTrackingState state, string json)
	{
		CombatTrackingSnapshot? snapshot = JsonSerializer.Deserialize<CombatTrackingSnapshot>(json);
		if (snapshot == null)
		{
			return;
		}

		RestoreSnapshot(state, snapshot);
	}

	public static void Clear(HextechMayhemCombatTrackingState state)
	{
		foreach (CombatTrackingFieldBinding binding in PersistentFieldBindings)
		{
			ClearStateField(binding.StateField, state);
		}

		foreach (FieldInfo field in TransientFields)
		{
			ClearStateField(field, state);
		}
	}

	private static CombatTrackingSnapshot CreateSnapshot(HextechMayhemCombatTrackingState state)
	{
		CombatTrackingSnapshot snapshot = new();
		foreach (CombatTrackingFieldBinding binding in PersistentFieldBindings)
		{
			binding.SnapshotProperty.SetValue(
				snapshot,
				CopyStateValue(binding.StateField.GetValue(state), binding.SnapshotProperty.PropertyType));
		}

		return snapshot;
	}

	private static void RestoreSnapshot(HextechMayhemCombatTrackingState state, CombatTrackingSnapshot snapshot)
	{
		foreach (CombatTrackingFieldBinding binding in PersistentFieldBindings)
		{
			RestoreStateField(binding.StateField, state, binding.SnapshotProperty.GetValue(snapshot));
		}
	}

	private static bool HasState(HextechMayhemCombatTrackingState state)
	{
		return PersistentFieldBindings.Any(binding => HasNonDefaultValue(binding.StateField.GetValue(state)));
	}
}

namespace HextechRunes;

internal static partial class HextechTelemetry
{
	public sealed record RuneChoiceRecord(
		int ActIndex,
		int PlayerSlot,
		string Rarity,
		IReadOnlyList<string> Options,
		string? Selected,
		int RerollCount);

	private sealed record TelemetryConfig(bool Enabled, string Endpoint);

	private sealed record RunEndedPayload(
		int SchemaVersion,
		string ModId,
		string ModVersion,
		string GameVersion,
		string UploadedAtUtc,
		RunTelemetry Run,
		IReadOnlyList<PlayerTelemetry> Players,
		IReadOnlyList<RuneChoiceRecord> RuneChoices,
		IReadOnlyList<MonsterHexTelemetry> MonsterHexes);

	private sealed record RunTelemetry(
		string RunId,
		string SeedHash,
		bool IsVictory,
		string NetMode,
		int PlayerCount,
		int Ascension,
		int CurrentActIndex,
		int TotalFloor,
		long RunTime);

	private sealed record PlayerTelemetry(
		int Slot,
		string Character,
		IReadOnlyList<string> HextechRunes);

	private sealed record MonsterHexTelemetry(
		int ActIndex,
		string Rarity,
		string Hex);
}

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public IReadOnlyList<HextechTelemetry.RuneChoiceRecord> GetTelemetryChoiceRecords()
	{
		return ChoiceHistory.GetTelemetryChoiceRecords();
	}

	public void RecordTelemetryChoice(HextechTelemetry.RuneChoiceRecord record)
	{
		ChoiceHistory.RecordTelemetryChoice(record);
	}

	public HashSet<ModelId> GetSeenPlayerRuneIds(Player player)
	{
		return ChoiceHistory.GetSeenPlayerRuneIds(player, RunState);
	}

	public void RecordSeenPlayerRunes(Player player, IEnumerable<RelicModel> relics)
	{
		ChoiceHistory.RecordSeenPlayerRunes(player, relics, RunState);
	}

	private string DescribePlayerHexCounts()
	{
		return HextechMayhemActRecovery.DescribePlayerHexCounts(RunState);
	}

	private string DescribeTelemetryChoiceCounts()
	{
		return HextechMayhemActRecovery.DescribeTelemetryChoiceCounts(ChoiceHistory);
	}
}

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	internal static T RequireCompletedSelection<T>(T? selected, string context)
		where T : class
	{
		return selected ?? throw new OperationCanceledException(
			$"Hextech selection ended without a confirmed choice: {context}");
	}

	private readonly record struct PendingRuneSelection(Player Player, List<RelicModel> Options, uint ChoiceId, bool IsLocal);

	// FinalMonsterHexes 只有本机界面(可调整敌方海克斯)的结果才带;远端还原的选择为 null。
	private readonly record struct RuneSelectionResult(
		RelicModel? SelectedRelic,
		IReadOnlyList<RelicModel> FinalOptions,
		int RerollCount,
		IReadOnlyList<MonsterHexKind>? FinalMonsterHexes = null)
	{
		public IReadOnlyList<MonsterHexKind> ResolvedMonsterHexes => FinalMonsterHexes ?? [];
	}

	private sealed class EnemyHexAdjustmentSyncContext(
		PlayerChoiceSynchronizer synchronizer,
		Player authorityPlayer,
		uint initialChoiceId,
		int actIndex,
		IReadOnlyList<MonsterHexKind> initialMonsterHexes)
	{
		public PlayerChoiceSynchronizer Synchronizer { get; } = synchronizer;
		public Player AuthorityPlayer { get; } = authorityPlayer;
		public uint NextChoiceId { get; set; } = initialChoiceId;
		public int ActIndex { get; } = actIndex;
		public int Sequence { get; set; }
		public IReadOnlyList<MonsterHexKind> InitialMonsterHexes { get; } = initialMonsterHexes.ToArray();
		public List<MonsterHexKind?> CurrentMonsterHexSlots { get; } = initialMonsterHexes.Select(static hex => (MonsterHexKind?)hex).ToList();
		public List<int> RerollCounts { get; } = initialMonsterHexes.Select(static _ => 0).ToList();
		public IReadOnlyList<MonsterHexKind> CurrentMonsterHexes => CurrentMonsterHexSlots
			.OfType<MonsterHexKind>()
			.ToArray();
		public bool FinalSent { get; set; }
		public Task? RemoteReceiveTask { get; set; }
	}
}

using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private readonly record struct PendingRuneSelection(Player Player, List<RelicModel> Options, uint ChoiceId, bool IsLocal);

	private readonly record struct RuneSelectionResult(
		RelicModel? SelectedRelic,
		IReadOnlyList<RelicModel> FinalOptions,
		int RerollCount,
		MonsterHexKind? FinalMonsterHex,
		HextechRuneSelectionScreen? BlockingScreen = null);

	private sealed class EnemyHexAdjustmentSyncContext(
		PlayerChoiceSynchronizer synchronizer,
		Player authorityPlayer,
		uint initialChoiceId,
		int actIndex,
		MonsterHexKind? initialMonsterHex)
	{
		public PlayerChoiceSynchronizer Synchronizer { get; } = synchronizer;
		public Player AuthorityPlayer { get; } = authorityPlayer;
		public uint NextChoiceId { get; set; } = initialChoiceId;
		public int ActIndex { get; } = actIndex;
		public int Sequence { get; set; }
		public MonsterHexKind? CurrentMonsterHex { get; set; } = initialMonsterHex;
		public bool Removed { get; set; }
		public int RerollCount { get; set; }
		public bool FinalSent { get; set; }
		public Task? RemoteReceiveTask { get; set; }
	}

	private const int FirstActSilverWeight = 20;
	private const int FirstActGoldWeight = 50;
	private const int FirstActPrismaticWeight = 30;
	private const int ActSelectionAppliedAckTimeoutFrames = 600;
	private const int RuneTagBiasBaseWeight = 100;
	private const int RuneTagBiasNormalBonusPerMatch = 25;
	private const int RuneTagBiasEndlessBonusPerMatch = 20;
	private const int RuneTagBiasMaxBonus = 50;
	private const int RuneTagBiasEndlessHistoryWindow = 3;
}

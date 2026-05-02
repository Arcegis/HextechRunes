using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

internal sealed class HextechMayhemCombatTrackingState
{
	public readonly Dictionary<uint, int> SlapProcsThisTurn = new();
	public readonly Dictionary<uint, int> TormentorProcsThisTurn = new();
	public readonly Dictionary<uint, int> CourageProcsThisTurn = new();
	public readonly Dictionary<uint, int> BloodPactProcsThisTurn = new();
	public readonly Dictionary<uint, int> ClownCollegeProcsThisTurn = new();
	public readonly HashSet<uint> EscapePlanTriggered = new();
	public readonly HashSet<uint> EscapePlanPending = new();
	public readonly HashSet<uint> RepulsorTriggered = new();
	public readonly HashSet<uint> RepulsorPending = new();
	public readonly HashSet<uint> DawnTriggered = new();
	public readonly HashSet<uint> SpeedDemonPending = new();
	public readonly HashSet<uint> DevilsDanceTriggeredThisTurn = new();
	public readonly HashSet<uint> FeelTheBurnTriggered = new();
	public readonly Dictionary<uint, uint> FeyMagicPendingNoDrawPlayers = new();
	public readonly Dictionary<uint, int> MikaelsBlessingTriggers = new();
	public readonly HashSet<uint> GoliathApplied = new();
	public readonly HashSet<uint> ProtectiveVeilApplied = new();
	public readonly HashSet<uint> ThornmailApplied = new();
	public readonly HashSet<uint> SuperBrainApplied = new();
	public readonly HashSet<uint> AstralBodyApplied = new();
	public readonly HashSet<uint> DrawYourSwordApplied = new();
	public readonly HashSet<uint> MadScientistApplied = new();
	public readonly HashSet<uint> UnmovableMountainApplied = new();
	public readonly HashSet<uint> GoldenSpatulaApplied = new();
	public readonly Dictionary<uint, int> TankEngineStacks = new();
	public readonly Dictionary<uint, int> ShrinkEngineStacks = new();
	public readonly Dictionary<uint, int> GetExcitedPending = new();
	public readonly HashSet<uint> FeelTheBurnPending = new();
	public readonly HashSet<uint> MountainSoulHasPreviousTurn = new();
	public readonly HashSet<uint> MountainSoulDamagedSinceLastTurn = new();
	public readonly Dictionary<ulong, int> PlayerAttackCardsPlayedThisCombat = new();
	public readonly Dictionary<ulong, int> PlayerCardsDrawnThisCombat = new();
	public readonly HashSet<ulong> EightPennyGatePlayersTriggeredThisTurn = new();
	public readonly Dictionary<ulong, int> EightPennyGatePendingCardHashes = new();
	public readonly HashSet<string> MonsterDebuffActionProcKeysThisTurn = new();
	public readonly HashSet<string> GroupedPlayerDebuffProcKeys = new();
	public string? LastEnemyThresholdTriggerKey;
	public bool HandlingMonsterTormentorBurn;
	public bool HandlingServantMasterIllusion;
	public bool HandlingGroupedPlayerDebuffs;
	public int EnemyProtectiveVeilTurnCounter;

	public string Serialize()
	{
		if (!HasState())
		{
			return "";
		}

		CombatTrackingSnapshot snapshot = new()
		{
			SlapProcsThisTurn = CopyDictionary(SlapProcsThisTurn),
			TormentorProcsThisTurn = CopyDictionary(TormentorProcsThisTurn),
			CourageProcsThisTurn = CopyDictionary(CourageProcsThisTurn),
			BloodPactProcsThisTurn = CopyDictionary(BloodPactProcsThisTurn),
			ClownCollegeProcsThisTurn = CopyDictionary(ClownCollegeProcsThisTurn),
			EscapePlanTriggered = CopySet(EscapePlanTriggered),
			EscapePlanPending = CopySet(EscapePlanPending),
			RepulsorTriggered = CopySet(RepulsorTriggered),
			RepulsorPending = CopySet(RepulsorPending),
			DawnTriggered = CopySet(DawnTriggered),
			SpeedDemonPending = CopySet(SpeedDemonPending),
			DevilsDanceTriggeredThisTurn = CopySet(DevilsDanceTriggeredThisTurn),
			FeelTheBurnTriggered = CopySet(FeelTheBurnTriggered),
			FeyMagicPendingNoDrawPlayers = CopyDictionary(FeyMagicPendingNoDrawPlayers),
			MikaelsBlessingTriggers = CopyDictionary(MikaelsBlessingTriggers),
			GoliathApplied = CopySet(GoliathApplied),
			ProtectiveVeilApplied = CopySet(ProtectiveVeilApplied),
			ThornmailApplied = CopySet(ThornmailApplied),
			SuperBrainApplied = CopySet(SuperBrainApplied),
			AstralBodyApplied = CopySet(AstralBodyApplied),
			DrawYourSwordApplied = CopySet(DrawYourSwordApplied),
			MadScientistApplied = CopySet(MadScientistApplied),
			UnmovableMountainApplied = CopySet(UnmovableMountainApplied),
			GoldenSpatulaApplied = CopySet(GoldenSpatulaApplied),
			TankEngineStacks = CopyDictionary(TankEngineStacks),
			ShrinkEngineStacks = CopyDictionary(ShrinkEngineStacks),
			GetExcitedPending = CopyDictionary(GetExcitedPending),
			FeelTheBurnPending = CopySet(FeelTheBurnPending),
			MountainSoulHasPreviousTurn = CopySet(MountainSoulHasPreviousTurn),
			MountainSoulDamagedSinceLastTurn = CopySet(MountainSoulDamagedSinceLastTurn),
			PlayerAttackCardsPlayedThisCombat = CopyDictionary(PlayerAttackCardsPlayedThisCombat),
			PlayerCardsDrawnThisCombat = CopyDictionary(PlayerCardsDrawnThisCombat),
			EightPennyGatePlayersTriggeredThisTurn = CopySet(EightPennyGatePlayersTriggeredThisTurn),
			EnemyProtectiveVeilTurnCounter = EnemyProtectiveVeilTurnCounter
		};
		return JsonSerializer.Serialize(snapshot);
	}

	public void Restore(string? json)
	{
		Clear();
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			CombatTrackingSnapshot? snapshot = JsonSerializer.Deserialize<CombatTrackingSnapshot>(json);
			if (snapshot == null)
			{
				return;
			}

			RestoreDictionary(SlapProcsThisTurn, snapshot.SlapProcsThisTurn);
			RestoreDictionary(TormentorProcsThisTurn, snapshot.TormentorProcsThisTurn);
			RestoreDictionary(CourageProcsThisTurn, snapshot.CourageProcsThisTurn);
			RestoreDictionary(BloodPactProcsThisTurn, snapshot.BloodPactProcsThisTurn);
			RestoreDictionary(ClownCollegeProcsThisTurn, snapshot.ClownCollegeProcsThisTurn);
			RestoreSet(EscapePlanTriggered, snapshot.EscapePlanTriggered);
			RestoreSet(EscapePlanPending, snapshot.EscapePlanPending);
			RestoreSet(RepulsorTriggered, snapshot.RepulsorTriggered);
			RestoreSet(RepulsorPending, snapshot.RepulsorPending);
			RestoreSet(DawnTriggered, snapshot.DawnTriggered);
			RestoreSet(SpeedDemonPending, snapshot.SpeedDemonPending);
			RestoreSet(DevilsDanceTriggeredThisTurn, snapshot.DevilsDanceTriggeredThisTurn);
			RestoreSet(FeelTheBurnTriggered, snapshot.FeelTheBurnTriggered);
			RestoreDictionary(FeyMagicPendingNoDrawPlayers, snapshot.FeyMagicPendingNoDrawPlayers);
			RestoreDictionary(MikaelsBlessingTriggers, snapshot.MikaelsBlessingTriggers);
			RestoreSet(GoliathApplied, snapshot.GoliathApplied);
			RestoreSet(ProtectiveVeilApplied, snapshot.ProtectiveVeilApplied);
			RestoreSet(ThornmailApplied, snapshot.ThornmailApplied);
			RestoreSet(SuperBrainApplied, snapshot.SuperBrainApplied);
			RestoreSet(AstralBodyApplied, snapshot.AstralBodyApplied);
			RestoreSet(DrawYourSwordApplied, snapshot.DrawYourSwordApplied);
			RestoreSet(MadScientistApplied, snapshot.MadScientistApplied);
			RestoreSet(UnmovableMountainApplied, snapshot.UnmovableMountainApplied);
			RestoreSet(GoldenSpatulaApplied, snapshot.GoldenSpatulaApplied);
			RestoreDictionary(TankEngineStacks, snapshot.TankEngineStacks);
			RestoreDictionary(ShrinkEngineStacks, snapshot.ShrinkEngineStacks);
			RestoreDictionary(GetExcitedPending, snapshot.GetExcitedPending);
			RestoreSet(FeelTheBurnPending, snapshot.FeelTheBurnPending);
			RestoreSet(MountainSoulHasPreviousTurn, snapshot.MountainSoulHasPreviousTurn);
			RestoreSet(MountainSoulDamagedSinceLastTurn, snapshot.MountainSoulDamagedSinceLastTurn);
			RestoreDictionary(PlayerAttackCardsPlayedThisCombat, snapshot.PlayerAttackCardsPlayedThisCombat);
			RestoreDictionary(PlayerCardsDrawnThisCombat, snapshot.PlayerCardsDrawnThisCombat);
			RestoreSet(EightPennyGatePlayersTriggeredThisTurn, snapshot.EightPennyGatePlayersTriggeredThisTurn);
			EnemyProtectiveVeilTurnCounter = Math.Max(0, snapshot.EnemyProtectiveVeilTurnCounter);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Failed to restore combat tracking snapshot: {ex}");
			Clear();
		}
	}

	public void Reset()
	{
		Clear();
	}

	private bool HasState()
	{
		return SlapProcsThisTurn.Count > 0
			|| TormentorProcsThisTurn.Count > 0
			|| CourageProcsThisTurn.Count > 0
			|| BloodPactProcsThisTurn.Count > 0
			|| ClownCollegeProcsThisTurn.Count > 0
			|| EscapePlanTriggered.Count > 0
			|| EscapePlanPending.Count > 0
			|| RepulsorTriggered.Count > 0
			|| RepulsorPending.Count > 0
			|| DawnTriggered.Count > 0
			|| SpeedDemonPending.Count > 0
			|| DevilsDanceTriggeredThisTurn.Count > 0
			|| FeelTheBurnTriggered.Count > 0
			|| FeyMagicPendingNoDrawPlayers.Count > 0
			|| MikaelsBlessingTriggers.Count > 0
			|| GoliathApplied.Count > 0
			|| ProtectiveVeilApplied.Count > 0
			|| ThornmailApplied.Count > 0
			|| SuperBrainApplied.Count > 0
			|| AstralBodyApplied.Count > 0
			|| DrawYourSwordApplied.Count > 0
			|| MadScientistApplied.Count > 0
			|| UnmovableMountainApplied.Count > 0
			|| GoldenSpatulaApplied.Count > 0
			|| TankEngineStacks.Count > 0
			|| ShrinkEngineStacks.Count > 0
			|| GetExcitedPending.Count > 0
			|| FeelTheBurnPending.Count > 0
			|| MountainSoulHasPreviousTurn.Count > 0
			|| MountainSoulDamagedSinceLastTurn.Count > 0
			|| PlayerAttackCardsPlayedThisCombat.Count > 0
			|| PlayerCardsDrawnThisCombat.Count > 0
			|| EightPennyGatePlayersTriggeredThisTurn.Count > 0
			|| EnemyProtectiveVeilTurnCounter > 0;
	}

	private void Clear()
	{
		SlapProcsThisTurn.Clear();
		TormentorProcsThisTurn.Clear();
		CourageProcsThisTurn.Clear();
		BloodPactProcsThisTurn.Clear();
		ClownCollegeProcsThisTurn.Clear();
		EscapePlanTriggered.Clear();
		EscapePlanPending.Clear();
		RepulsorTriggered.Clear();
		RepulsorPending.Clear();
		DawnTriggered.Clear();
		SpeedDemonPending.Clear();
		DevilsDanceTriggeredThisTurn.Clear();
		FeelTheBurnTriggered.Clear();
		FeyMagicPendingNoDrawPlayers.Clear();
		MikaelsBlessingTriggers.Clear();
		GoliathApplied.Clear();
		ProtectiveVeilApplied.Clear();
		ThornmailApplied.Clear();
		SuperBrainApplied.Clear();
		AstralBodyApplied.Clear();
		DrawYourSwordApplied.Clear();
		MadScientistApplied.Clear();
		UnmovableMountainApplied.Clear();
		GoldenSpatulaApplied.Clear();
		TankEngineStacks.Clear();
		ShrinkEngineStacks.Clear();
		GetExcitedPending.Clear();
		FeelTheBurnPending.Clear();
		MountainSoulHasPreviousTurn.Clear();
		MountainSoulDamagedSinceLastTurn.Clear();
		PlayerAttackCardsPlayedThisCombat.Clear();
		PlayerCardsDrawnThisCombat.Clear();
		EightPennyGatePlayersTriggeredThisTurn.Clear();
		EightPennyGatePendingCardHashes.Clear();
		MonsterDebuffActionProcKeysThisTurn.Clear();
		GroupedPlayerDebuffProcKeys.Clear();
		LastEnemyThresholdTriggerKey = null;
		EnemyProtectiveVeilTurnCounter = 0;
		HandlingMonsterTormentorBurn = false;
		HandlingServantMasterIllusion = false;
		HandlingGroupedPlayerDebuffs = false;
	}

	private static Dictionary<TKey, TValue> CopyDictionary<TKey, TValue>(Dictionary<TKey, TValue> source)
		where TKey : notnull
	{
		return source.Count == 0
			? new Dictionary<TKey, TValue>()
			: source.OrderBy(static item => item.Key).ToDictionary(static item => item.Key, static item => item.Value);
	}

	private static List<T> CopySet<T>(HashSet<T> source)
	{
		return source.Count == 0 ? [] : source.OrderBy(static item => item).ToList();
	}

	private static void RestoreDictionary<TKey, TValue>(Dictionary<TKey, TValue> target, Dictionary<TKey, TValue>? source)
		where TKey : notnull
	{
		target.Clear();
		if (source == null)
		{
			return;
		}

		foreach (KeyValuePair<TKey, TValue> pair in source)
		{
			target[pair.Key] = pair.Value;
		}
	}

	private static void RestoreSet<T>(HashSet<T> target, IEnumerable<T>? source)
	{
		target.Clear();
		if (source == null)
		{
			return;
		}

		foreach (T value in source)
		{
			target.Add(value);
		}
	}

	private sealed class CombatTrackingSnapshot
	{
		public Dictionary<uint, int> SlapProcsThisTurn { get; set; } = new();
		public Dictionary<uint, int> TormentorProcsThisTurn { get; set; } = new();
		public Dictionary<uint, int> CourageProcsThisTurn { get; set; } = new();
		public Dictionary<uint, int> BloodPactProcsThisTurn { get; set; } = new();
		public Dictionary<uint, int> ClownCollegeProcsThisTurn { get; set; } = new();
		public List<uint> EscapePlanTriggered { get; set; } = [];
		public List<uint> EscapePlanPending { get; set; } = [];
		public List<uint> RepulsorTriggered { get; set; } = [];
		public List<uint> RepulsorPending { get; set; } = [];
		public List<uint> DawnTriggered { get; set; } = [];
		public List<uint> SpeedDemonPending { get; set; } = [];
		public List<uint> DevilsDanceTriggeredThisTurn { get; set; } = [];
		public List<uint> FeelTheBurnTriggered { get; set; } = [];
		public Dictionary<uint, uint> FeyMagicPendingNoDrawPlayers { get; set; } = new();
		public Dictionary<uint, int> MikaelsBlessingTriggers { get; set; } = new();
		public List<uint> GoliathApplied { get; set; } = [];
		public List<uint> ProtectiveVeilApplied { get; set; } = [];
		public List<uint> ThornmailApplied { get; set; } = [];
		public List<uint> SuperBrainApplied { get; set; } = [];
		public List<uint> AstralBodyApplied { get; set; } = [];
		public List<uint> DrawYourSwordApplied { get; set; } = [];
		public List<uint> MadScientistApplied { get; set; } = [];
		public List<uint> UnmovableMountainApplied { get; set; } = [];
		public List<uint> GoldenSpatulaApplied { get; set; } = [];
		public Dictionary<uint, int> TankEngineStacks { get; set; } = new();
		public Dictionary<uint, int> ShrinkEngineStacks { get; set; } = new();
		public Dictionary<uint, int> GetExcitedPending { get; set; } = new();
		public List<uint> FeelTheBurnPending { get; set; } = [];
		public List<uint> MountainSoulHasPreviousTurn { get; set; } = [];
		public List<uint> MountainSoulDamagedSinceLastTurn { get; set; } = [];
		public Dictionary<ulong, int> PlayerAttackCardsPlayedThisCombat { get; set; } = new();
		public Dictionary<ulong, int> PlayerCardsDrawnThisCombat { get; set; } = new();
		public List<ulong> EightPennyGatePlayersTriggeredThisTurn { get; set; } = [];
		public int EnemyProtectiveVeilTurnCounter { get; set; }
	}
}

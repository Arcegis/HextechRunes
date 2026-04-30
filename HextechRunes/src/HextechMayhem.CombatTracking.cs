using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private string SerializeCombatTracking()
	{
		if (!HasCombatTrackingState())
		{
			return "";
		}

		CombatTrackingSnapshot snapshot = new()
		{
			SlapProcsThisTurn = CopyDictionary(_slapProcsThisTurn),
			TormentorProcsThisTurn = CopyDictionary(_tormentorProcsThisTurn),
			CourageProcsThisTurn = CopyDictionary(_courageProcsThisTurn),
			BloodPactProcsThisTurn = CopyDictionary(_bloodPactProcsThisTurn),
			ClownCollegeProcsThisTurn = CopyDictionary(_clownCollegeProcsThisTurn),
			EscapePlanTriggered = CopySet(_escapePlanTriggered),
			EscapePlanPending = CopySet(_escapePlanPending),
			RepulsorTriggered = CopySet(_repulsorTriggered),
			RepulsorPending = CopySet(_repulsorPending),
			DawnTriggered = CopySet(_dawnTriggered),
			SpeedDemonPending = CopySet(_speedDemonPending),
			DevilsDanceTriggeredThisTurn = CopySet(_devilsDanceTriggeredThisTurn),
			FeelTheBurnTriggered = CopySet(_feelTheBurnTriggered),
			FeyMagicPendingNoDrawPlayers = CopyDictionary(_feyMagicPendingNoDrawPlayers),
			MikaelsBlessingTriggers = CopyDictionary(_mikaelsBlessingTriggers),
			GoliathApplied = CopySet(_goliathApplied),
			ProtectiveVeilApplied = CopySet(_protectiveVeilApplied),
			ThornmailApplied = CopySet(_thornmailApplied),
			SuperBrainApplied = CopySet(_superBrainApplied),
			AstralBodyApplied = CopySet(_astralBodyApplied),
			DrawYourSwordApplied = CopySet(_drawYourSwordApplied),
			MadScientistApplied = CopySet(_madScientistApplied),
			UnmovableMountainApplied = CopySet(_unmovableMountainApplied),
			GoldenSpatulaApplied = CopySet(_goldenSpatulaApplied),
			TankEngineStacks = CopyDictionary(_tankEngineStacks),
			ShrinkEngineStacks = CopyDictionary(_shrinkEngineStacks),
			GetExcitedPending = CopyDictionary(_getExcitedPending),
			FeelTheBurnPending = CopySet(_feelTheBurnPending),
			MountainSoulHasPreviousTurn = CopySet(_mountainSoulHasPreviousTurn),
			MountainSoulDamagedSinceLastTurn = CopySet(_mountainSoulDamagedSinceLastTurn),
			PlayerAttackCardsPlayedThisCombat = CopyDictionary(_playerAttackCardsPlayedThisCombat),
			EnemyProtectiveVeilTurnCounter = _enemyProtectiveVeilTurnCounter
		};
		return JsonSerializer.Serialize(snapshot);
	}

	private void RestoreCombatTracking(string? json)
	{
		ClearCombatTrackingState();
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

			RestoreDictionary(_slapProcsThisTurn, snapshot.SlapProcsThisTurn);
			RestoreDictionary(_tormentorProcsThisTurn, snapshot.TormentorProcsThisTurn);
			RestoreDictionary(_courageProcsThisTurn, snapshot.CourageProcsThisTurn);
			RestoreDictionary(_bloodPactProcsThisTurn, snapshot.BloodPactProcsThisTurn);
			RestoreDictionary(_clownCollegeProcsThisTurn, snapshot.ClownCollegeProcsThisTurn);
			RestoreSet(_escapePlanTriggered, snapshot.EscapePlanTriggered);
			RestoreSet(_escapePlanPending, snapshot.EscapePlanPending);
			RestoreSet(_repulsorTriggered, snapshot.RepulsorTriggered);
			RestoreSet(_repulsorPending, snapshot.RepulsorPending);
			RestoreSet(_dawnTriggered, snapshot.DawnTriggered);
			RestoreSet(_speedDemonPending, snapshot.SpeedDemonPending);
			RestoreSet(_devilsDanceTriggeredThisTurn, snapshot.DevilsDanceTriggeredThisTurn);
			RestoreSet(_feelTheBurnTriggered, snapshot.FeelTheBurnTriggered);
			RestoreDictionary(_feyMagicPendingNoDrawPlayers, snapshot.FeyMagicPendingNoDrawPlayers);
			RestoreDictionary(_mikaelsBlessingTriggers, snapshot.MikaelsBlessingTriggers);
			RestoreSet(_goliathApplied, snapshot.GoliathApplied);
			RestoreSet(_protectiveVeilApplied, snapshot.ProtectiveVeilApplied);
			RestoreSet(_thornmailApplied, snapshot.ThornmailApplied);
			RestoreSet(_superBrainApplied, snapshot.SuperBrainApplied);
			RestoreSet(_astralBodyApplied, snapshot.AstralBodyApplied);
			RestoreSet(_drawYourSwordApplied, snapshot.DrawYourSwordApplied);
			RestoreSet(_madScientistApplied, snapshot.MadScientistApplied);
			RestoreSet(_unmovableMountainApplied, snapshot.UnmovableMountainApplied);
			RestoreSet(_goldenSpatulaApplied, snapshot.GoldenSpatulaApplied);
			RestoreDictionary(_tankEngineStacks, snapshot.TankEngineStacks);
			RestoreDictionary(_shrinkEngineStacks, snapshot.ShrinkEngineStacks);
			RestoreDictionary(_getExcitedPending, snapshot.GetExcitedPending);
			RestoreSet(_feelTheBurnPending, snapshot.FeelTheBurnPending);
			RestoreSet(_mountainSoulHasPreviousTurn, snapshot.MountainSoulHasPreviousTurn);
			RestoreSet(_mountainSoulDamagedSinceLastTurn, snapshot.MountainSoulDamagedSinceLastTurn);
			RestoreDictionary(_playerAttackCardsPlayedThisCombat, snapshot.PlayerAttackCardsPlayedThisCombat);
			_enemyProtectiveVeilTurnCounter = Math.Max(0, snapshot.EnemyProtectiveVeilTurnCounter);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Failed to restore combat tracking snapshot: {ex}");
			ClearCombatTrackingState();
		}
	}

	private bool HasCombatTrackingState()
	{
		return _slapProcsThisTurn.Count > 0
			|| _tormentorProcsThisTurn.Count > 0
			|| _courageProcsThisTurn.Count > 0
			|| _bloodPactProcsThisTurn.Count > 0
			|| _clownCollegeProcsThisTurn.Count > 0
			|| _escapePlanTriggered.Count > 0
			|| _escapePlanPending.Count > 0
			|| _repulsorTriggered.Count > 0
			|| _repulsorPending.Count > 0
			|| _dawnTriggered.Count > 0
			|| _speedDemonPending.Count > 0
			|| _devilsDanceTriggeredThisTurn.Count > 0
			|| _feelTheBurnTriggered.Count > 0
			|| _feyMagicPendingNoDrawPlayers.Count > 0
			|| _mikaelsBlessingTriggers.Count > 0
			|| _goliathApplied.Count > 0
			|| _protectiveVeilApplied.Count > 0
			|| _thornmailApplied.Count > 0
			|| _superBrainApplied.Count > 0
			|| _astralBodyApplied.Count > 0
			|| _drawYourSwordApplied.Count > 0
			|| _madScientistApplied.Count > 0
			|| _unmovableMountainApplied.Count > 0
			|| _goldenSpatulaApplied.Count > 0
			|| _tankEngineStacks.Count > 0
			|| _shrinkEngineStacks.Count > 0
			|| _getExcitedPending.Count > 0
			|| _feelTheBurnPending.Count > 0
			|| _mountainSoulHasPreviousTurn.Count > 0
			|| _mountainSoulDamagedSinceLastTurn.Count > 0
			|| _playerAttackCardsPlayedThisCombat.Count > 0
			|| _enemyProtectiveVeilTurnCounter > 0;
	}

	private void ResetCombatTracking()
	{
		ClearCombatTrackingState();
	}

	private void ClearCombatTrackingState()
	{
		_slapProcsThisTurn.Clear();
		_tormentorProcsThisTurn.Clear();
		_courageProcsThisTurn.Clear();
		_bloodPactProcsThisTurn.Clear();
		_clownCollegeProcsThisTurn.Clear();
		_escapePlanTriggered.Clear();
		_escapePlanPending.Clear();
		_repulsorTriggered.Clear();
		_repulsorPending.Clear();
		_dawnTriggered.Clear();
		_speedDemonPending.Clear();
		_devilsDanceTriggeredThisTurn.Clear();
		_feelTheBurnTriggered.Clear();
		_feyMagicPendingNoDrawPlayers.Clear();
		_mikaelsBlessingTriggers.Clear();
		_goliathApplied.Clear();
		_protectiveVeilApplied.Clear();
		_thornmailApplied.Clear();
		_superBrainApplied.Clear();
		_astralBodyApplied.Clear();
		_drawYourSwordApplied.Clear();
		_madScientistApplied.Clear();
		_unmovableMountainApplied.Clear();
		_goldenSpatulaApplied.Clear();
		_tankEngineStacks.Clear();
		_shrinkEngineStacks.Clear();
		_getExcitedPending.Clear();
		_feelTheBurnPending.Clear();
		_mountainSoulHasPreviousTurn.Clear();
		_mountainSoulDamagedSinceLastTurn.Clear();
		_playerAttackCardsPlayedThisCombat.Clear();
		_monsterDebuffActionProcKeysThisTurn.Clear();
		_groupedPlayerDebuffProcKeys.Clear();
		_lastEnemyThresholdTriggerKey = null;
		_enemyProtectiveVeilTurnCounter = 0;
		_handlingMonsterTormentorBurn = false;
		_handlingServantMasterIllusion = false;
		_handlingGroupedPlayerDebuffs = false;
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
		public int EnemyProtectiveVeilTurnCounter { get; set; }
	}
}

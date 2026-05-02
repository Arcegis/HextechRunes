using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private readonly HextechMayhemActState _actState = new();
	private string _telemetryChoicesJson = "";
	private string _seenPlayerRuneIdsJson = "";
	private readonly object _seenPlayerRuneIdsLock = new();

	private readonly Dictionary<uint, int> _slapProcsThisTurn = new();
	private readonly Dictionary<uint, int> _tormentorProcsThisTurn = new();
	private readonly Dictionary<uint, int> _courageProcsThisTurn = new();
	private readonly Dictionary<uint, int> _bloodPactProcsThisTurn = new();
	private readonly Dictionary<uint, int> _clownCollegeProcsThisTurn = new();
	private readonly HashSet<uint> _escapePlanTriggered = new();
	private readonly HashSet<uint> _escapePlanPending = new();
	private readonly HashSet<uint> _repulsorTriggered = new();
	private readonly HashSet<uint> _repulsorPending = new();
	private readonly HashSet<uint> _dawnTriggered = new();
	private readonly HashSet<uint> _speedDemonPending = new();
	private readonly HashSet<uint> _devilsDanceTriggeredThisTurn = new();
	private readonly HashSet<uint> _feelTheBurnTriggered = new();
	private readonly Dictionary<uint, uint> _feyMagicPendingNoDrawPlayers = new();
	private readonly Dictionary<uint, int> _mikaelsBlessingTriggers = new();
	private readonly HashSet<uint> _goliathApplied = new();
	private readonly HashSet<uint> _protectiveVeilApplied = new();
	private readonly HashSet<uint> _thornmailApplied = new();
	private readonly HashSet<uint> _superBrainApplied = new();
	private readonly HashSet<uint> _astralBodyApplied = new();
	private readonly HashSet<uint> _drawYourSwordApplied = new();
	private readonly HashSet<uint> _madScientistApplied = new();
	private readonly HashSet<uint> _unmovableMountainApplied = new();
	private readonly HashSet<uint> _goldenSpatulaApplied = new();
	private readonly Dictionary<uint, int> _tankEngineStacks = new();
	private readonly Dictionary<uint, int> _shrinkEngineStacks = new();
	private readonly Dictionary<uint, int> _getExcitedPending = new();
	private readonly HashSet<uint> _feelTheBurnPending = new();
	private readonly HashSet<uint> _mountainSoulHasPreviousTurn = new();
	private readonly HashSet<uint> _mountainSoulDamagedSinceLastTurn = new();
	private readonly Dictionary<ulong, int> _playerAttackCardsPlayedThisCombat = new();
	private readonly Dictionary<ulong, int> _playerCardsDrawnThisCombat = new();
	private readonly HashSet<ulong> _eightPennyGatePlayersTriggeredThisTurn = new();
	private readonly Dictionary<ulong, int> _eightPennyGatePendingCardHashes = new();
	private readonly HashSet<string> _monsterDebuffActionProcKeysThisTurn = new();
	private readonly HashSet<string> _groupedPlayerDebuffProcKeys = new();
	private string? _lastEnemyThresholdTriggerKey;
	private bool _handlingMonsterTormentorBurn;
	private bool _handlingServantMasterIllusion;
	private bool _handlingGroupedPlayerDebuffs;
	private int _enemyProtectiveVeilTurnCounter;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedRarityByAct
	{
		get => _actState.SavedRarityByAct;
		set => _actState.SavedRarityByAct = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedMonsterHexByAct
	{
		get => _actState.SavedMonsterHexByAct;
		set => _actState.SavedMonsterHexByAct = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int[] SavedResolvedActs
	{
		get => _actState.SavedResolvedActs;
		set => _actState.SavedResolvedActs = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedTelemetryChoicesJson
	{
		get => _telemetryChoicesJson;
		set => _telemetryChoicesJson = value ?? "";
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedSeenPlayerRuneIdsJson
	{
		get => _seenPlayerRuneIdsJson;
		set => _seenPlayerRuneIdsJson = value ?? "";
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedCombatTrackingJson
	{
		get => SerializeCombatTracking();
		set => RestoreCombatTracking(value);
	}

	public override LocString Title => new("modifiers", "HEXTECH_MAYHEM.title");

	public override LocString Description => new("modifiers", "HEXTECH_MAYHEM.description");

	protected override string IconPath => ImageHelper.GetImagePath("powers/missing_power.png");

	public override IEnumerable<IHoverTip> HoverTips => [];

	public RunState ActiveRunState => RunState;

	public bool IsActResolved(int actIndex)
	{
		return _actState.IsResolved(actIndex);
	}

	public void SetActResolved(int actIndex, bool resolved)
	{
		_actState.SetResolved(actIndex, resolved);
	}

	public bool TryRecoverResolvedActsFromPlayerRelics(string reason)
	{
		int currentActIndex = Math.Min(RunState.CurrentActIndex, _actState.ActCount - 1);
		if (currentActIndex < 0 || RunState.Players.Count == 0)
		{
			return false;
		}

		int telemetryRecoverThroughAct = GetHighestActResolvedByTelemetryChoices(currentActIndex);
		int countRecoverThroughAct = GetHighestActResolvedByPlayerRuneCounts(currentActIndex == 0 ? 0 : currentActIndex - 1);
		int recoverThroughAct = Math.Max(telemetryRecoverThroughAct, countRecoverThroughAct);
		if (recoverThroughAct < 0)
		{
			return false;
		}

		bool changed = false;
		for (int actIndex = 0; actIndex <= recoverThroughAct; actIndex++)
		{
			changed |= _actState.TryMarkResolved(actIndex);

			if (TryInferRarityForAct(actIndex, out HextechRarityTier rarity))
			{
				changed |= _actState.TrySetRarityIfMissing(actIndex, rarity);
			}
		}

		if (changed)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] Recovered resolved acts from saved choices/player relics: reason={reason} currentAct={RunState.CurrentActIndex} recoverThrough={recoverThroughAct} telemetryThrough={telemetryRecoverThroughAct} countThrough={countRecoverThroughAct} {_actState.Describe()} counts={DescribePlayerHexCounts()} choices={DescribeTelemetryChoiceCounts()}");
		}

		return changed;
	}

	public string DescribeActState()
	{
		return _actState.Describe();
	}

	public HextechRarityTier? GetRarityForAct(int actIndex)
	{
		return _actState.GetRarity(actIndex);
	}

	public void SetRarityForAct(int actIndex, HextechRarityTier rarity)
	{
		_actState.SetRarity(actIndex, rarity);
	}

	public MonsterHexKind? GetMonsterHexForAct(int actIndex)
	{
		return _actState.GetMonsterHex(actIndex);
	}

	public void SetMonsterHexForAct(int actIndex, MonsterHexKind hex)
	{
		_actState.SetMonsterHex(actIndex, hex);
	}

	public IReadOnlyList<MonsterHexKind> GetActiveMonsterHexes()
	{
		return _actState.GetActiveMonsterHexes(RunState.CurrentActIndex, ShouldRecoverMonsterHexInCombat);
	}

	private bool ShouldRecoverMonsterHexInCombat(int actIndex)
	{
		return actIndex <= RunState.CurrentActIndex && RunState.CurrentRoom is CombatRoom;
	}

	public void ResetForNewRun()
	{
		_actState.Reset();
		_telemetryChoicesJson = "";
		_seenPlayerRuneIdsJson = "";
		ResetCombatTracking();
	}

	public void DebugSetOnlyMonsterHex(int actIndex, MonsterHexKind hex, HextechRarityTier rarity)
	{
		_actState.DebugSetOnlyMonsterHex(actIndex, hex, rarity);
		_telemetryChoicesJson = "";
		_seenPlayerRuneIdsJson = "";

		ResetCombatTracking();
	}

	public bool HasActiveMonsterHex(MonsterHexKind hex)
	{
		return GetActiveMonsterHexes().Contains(hex);
	}

	public IReadOnlyList<HextechTelemetry.RuneChoiceRecord> GetTelemetryChoiceRecords()
	{
		if (string.IsNullOrWhiteSpace(_telemetryChoicesJson))
		{
			return [];
		}

		try
		{
			return JsonSerializer.Deserialize<List<HextechTelemetry.RuneChoiceRecord>>(_telemetryChoicesJson, HextechTelemetry.JsonOptions) ?? [];
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Telemetry choices decode failed: {ex.Message}");
			return [];
		}
	}

	public void RecordTelemetryChoice(HextechTelemetry.RuneChoiceRecord record)
	{
		List<HextechTelemetry.RuneChoiceRecord> records = GetTelemetryChoiceRecords().ToList();
		records.RemoveAll(existing => existing.ActIndex == record.ActIndex && existing.PlayerSlot == record.PlayerSlot);
		records.Add(record);
		_telemetryChoicesJson = JsonSerializer.Serialize(records, HextechTelemetry.JsonOptions);
	}

	public HashSet<ModelId> GetSeenPlayerRuneIds(Player player)
	{
		int playerSlot = GetPlayerSlotIndex(player);
		HashSet<string> entries;
		lock (_seenPlayerRuneIdsLock)
		{
			entries = GetSeenPlayerRuneEntries(playerSlot);
		}

		foreach (string entry in GetTelemetryChoiceRecords()
			.Where(record => record.PlayerSlot == playerSlot)
			.SelectMany(static record => record.Options))
		{
			if (!string.IsNullOrWhiteSpace(entry))
			{
				entries.Add(entry);
			}
		}

		HashSet<ModelId> result = [];
		foreach (string entry in entries)
		{
			try
			{
				result.Add(new ModelId(ModInfo.Id, entry));
			}
			catch (Exception ex)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] Seen player rune id ignored: slot={playerSlot} entry={entry} error={ex.Message}");
			}
		}

		return result;
	}

	public void RecordSeenPlayerRunes(Player player, IEnumerable<RelicModel> relics)
	{
		List<string> entriesToAdd = [];
		foreach (RelicModel relic in relics)
		{
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			if (ModInfo.IsHextechRelic(relic) && !string.IsNullOrWhiteSpace(id.Entry))
			{
				entriesToAdd.Add(id.Entry);
			}
		}

		if (entriesToAdd.Count == 0)
		{
			return;
		}

		int playerSlot = GetPlayerSlotIndex(player);
		lock (_seenPlayerRuneIdsLock)
		{
			Dictionary<int, HashSet<string>> seenBySlot = DecodeSeenPlayerRuneEntries();
			if (!seenBySlot.TryGetValue(playerSlot, out HashSet<string>? seenEntries))
			{
				seenEntries = new HashSet<string>(StringComparer.Ordinal);
				seenBySlot[playerSlot] = seenEntries;
			}

			bool changed = false;
			foreach (string entry in entriesToAdd)
			{
				changed |= seenEntries.Add(entry);
			}

			if (changed)
			{
				_seenPlayerRuneIdsJson = JsonSerializer.Serialize(
					seenBySlot.ToDictionary(
						static pair => pair.Key.ToString(),
						static pair => pair.Value.OrderBy(static entry => entry, StringComparer.Ordinal).ToArray()),
					HextechTelemetry.JsonOptions);
			}
		}
	}

	private HashSet<string> GetSeenPlayerRuneEntries(int playerSlot)
	{
		Dictionary<int, HashSet<string>> seenBySlot = DecodeSeenPlayerRuneEntries();
		if (seenBySlot.TryGetValue(playerSlot, out HashSet<string>? entries))
		{
			return entries.ToHashSet(StringComparer.Ordinal);
		}

		return new HashSet<string>(StringComparer.Ordinal);
	}

	private Dictionary<int, HashSet<string>> DecodeSeenPlayerRuneEntries()
	{
		Dictionary<int, HashSet<string>> result = [];
		if (string.IsNullOrWhiteSpace(_seenPlayerRuneIdsJson))
		{
			return result;
		}

		try
		{
			Dictionary<string, string[]>? decoded = JsonSerializer.Deserialize<Dictionary<string, string[]>>(_seenPlayerRuneIdsJson, HextechTelemetry.JsonOptions);
			if (decoded == null)
			{
				return result;
			}

			foreach ((string slotText, string[] entries) in decoded)
			{
				if (int.TryParse(slotText, out int slot))
				{
					result[slot] = entries
						.Where(static entry => !string.IsNullOrWhiteSpace(entry))
						.ToHashSet(StringComparer.Ordinal);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Seen player rune ids decode failed: {ex.Message}");
		}

		return result;
	}

	private int GetPlayerSlotIndex(Player player)
	{
		int slot = RunState.GetPlayerSlotIndex(player);
		if (slot >= 0)
		{
			return slot;
		}

		for (int i = 0; i < RunState.Players.Count; i++)
		{
			if (ReferenceEquals(RunState.Players[i], player))
			{
				return i;
			}
		}

		return 0;
	}

	private int GetHighestActResolvedByTelemetryChoices(int maxActIndex)
	{
		int lastActIndex = _actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0 || RunState.Players.Count == 0)
		{
			return -1;
		}

		IReadOnlyList<HextechTelemetry.RuneChoiceRecord> records = GetTelemetryChoiceRecords();
		if (records.Count == 0)
		{
			return -1;
		}

		int highest = -1;
		for (int actIndex = 0; actIndex <= lastActIndex; actIndex++)
		{
			HashSet<int> playerSlots = records
				.Where(record => record.ActIndex == actIndex)
				.Select(static record => record.PlayerSlot)
				.ToHashSet();
			bool allPlayersRecorded = true;
			for (int playerSlot = 0; playerSlot < RunState.Players.Count; playerSlot++)
			{
				if (!playerSlots.Contains(playerSlot))
				{
					allPlayersRecorded = false;
					break;
				}
			}

			if (!allPlayersRecorded)
			{
				break;
			}

			highest = actIndex;
		}

		return highest;
	}

	private int GetHighestActResolvedByPlayerRuneCounts(int maxActIndex)
	{
		int lastActIndex = _actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0)
		{
			return -1;
		}

		int minHexCount = int.MaxValue;
		foreach (Player player in RunState.Players)
		{
			int count = player.Relics.Count(ModInfo.IsHextechRelic);
			minHexCount = Math.Min(minHexCount, count);
		}

		if (minHexCount == int.MaxValue || minHexCount <= 0)
		{
			return -1;
		}

		return Math.Min(lastActIndex, minHexCount - 1);
	}

	private bool TryInferRarityForAct(int actIndex, out HextechRarityTier rarity)
	{
		return TryInferRarityForActFromTelemetryChoices(actIndex, out rarity)
			|| TryInferRarityForActFromPlayerRelics(actIndex, out rarity);
	}

	private bool TryInferRarityForActFromTelemetryChoices(int actIndex, out HextechRarityTier rarity)
	{
		foreach (HextechTelemetry.RuneChoiceRecord record in GetTelemetryChoiceRecords().Where(record => record.ActIndex == actIndex))
		{
			if (Enum.TryParse(record.Rarity, ignoreCase: true, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}

	private bool TryInferRarityForActFromPlayerRelics(int actIndex, out HextechRarityTier rarity)
	{
		foreach (Player player in RunState.Players)
		{
			RelicModel? relic = player.Relics
				.Where(ModInfo.IsHextechRelic)
				.ElementAtOrDefault(actIndex);
			if (ModInfo.TryGetPlayerRuneRarity(relic, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}

	private string DescribePlayerHexCounts()
	{
		return string.Join(",", RunState.Players.Select(player => $"{player.NetId}:{player.Relics.Count(ModInfo.IsHextechRelic)}"));
	}

	private string DescribeTelemetryChoiceCounts()
	{
		return string.Join(",", GetTelemetryChoiceRecords()
			.GroupBy(static record => record.ActIndex)
			.OrderBy(static group => group.Key)
			.Select(static group => $"{group.Key}:{group.Count()}"));
	}
}

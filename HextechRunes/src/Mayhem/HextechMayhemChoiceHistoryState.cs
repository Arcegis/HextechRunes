using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed class HextechMayhemChoiceHistoryState
{
	private readonly object _seenPlayerRuneIdsLock = new();
	private string _telemetryChoicesJson = "";
	private string _seenPlayerRuneIdsJson = "";

	public string SavedTelemetryChoicesJson
	{
		get => _telemetryChoicesJson;
		set => _telemetryChoicesJson = value ?? "";
	}

	public string SavedSeenPlayerRuneIdsJson
	{
		get => _seenPlayerRuneIdsJson;
		set => _seenPlayerRuneIdsJson = value ?? "";
	}

	public void Reset()
	{
		_telemetryChoicesJson = "";
		_seenPlayerRuneIdsJson = "";
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
		records.RemoveAll(existing =>
			existing.ActIndex == record.ActIndex
			&& existing.PlayerSlot == record.PlayerSlot
			&& existing.ChoiceOrdinal == record.ChoiceOrdinal);
		records.Add(record);
		_telemetryChoicesJson = JsonSerializer.Serialize(records, HextechTelemetry.JsonOptions);
	}

	public HashSet<ModelId> GetSeenPlayerRuneIds(Player player, RunState runState)
	{
		int playerSlot = GetPlayerSlotIndex(player, runState);
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

	public void RecordSeenPlayerRunes(Player player, IEnumerable<RelicModel> relics, RunState runState)
	{
		List<string> entriesToAdd = [];
		foreach (RelicModel relic in relics)
		{
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			if (HextechCatalog.IsHextechRelic(relic) && !string.IsNullOrWhiteSpace(id.Entry))
			{
				entriesToAdd.Add(id.Entry);
			}
		}

		if (entriesToAdd.Count == 0)
		{
			return;
		}

		int playerSlot = GetPlayerSlotIndex(player, runState);
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

	private static int GetPlayerSlotIndex(Player player, RunState runState)
	{
		int slot = runState.GetPlayerSlotIndex(player);
		if (slot >= 0)
		{
			return slot;
		}

		for (int i = 0; i < runState.Players.Count; i++)
		{
			if (ReferenceEquals(runState.Players[i], player))
			{
				return i;
			}
		}

		return 0;
	}
}

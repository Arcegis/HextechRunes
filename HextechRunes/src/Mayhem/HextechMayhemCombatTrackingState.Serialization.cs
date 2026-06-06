using System;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

internal sealed partial class HextechMayhemCombatTrackingState
{
	public string Serialize()
	{
		return HextechMayhemCombatTrackingSerializer.Serialize(this);
	}

	public void Restore(string? json)
	{
		HextechMayhemCombatTrackingSerializer.Clear(this);
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			HextechMayhemCombatTrackingSerializer.Restore(this, json);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Failed to restore combat tracking snapshot: {ex}");
			HextechMayhemCombatTrackingSerializer.Clear(this);
		}
	}

	private void Clear()
	{
		HextechMayhemCombatTrackingSerializer.Clear(this);
	}
}

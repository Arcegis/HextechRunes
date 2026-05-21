using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechMultiplayerScalingCompat
{
	private const string BetterMultiplayerScalingAssemblyName = "BetterMultiplayerScaling";
	private static bool _warnedClientExtraScaling;
	private static bool _warnedHostOnlyScaling;

	public static bool IsBetterMultiplayerScalingLoaded()
	{
		return AppDomain.CurrentDomain.GetAssemblies().Any(static assembly =>
			string.Equals(assembly.GetName().Name, BetterMultiplayerScalingAssemblyName, StringComparison.OrdinalIgnoreCase));
	}

	public static void RefreshHostScalingFlagForLocalHost(HextechMayhemModifier modifier)
	{
		if (RunManager.Instance?.NetService?.Type == NetGameType.Host)
		{
			modifier.HostUsesBetterMultiplayerScaling = IsBetterMultiplayerScalingLoaded();
		}
	}

	public static async Task NormalizeCombatEnemyHpIfNeeded(HextechMayhemModifier modifier, CombatRoom room)
	{
		foreach (Creature enemy in room.CombatState.Enemies.Where(static creature => creature.Side == CombatSide.Enemy && creature.IsAlive).ToList())
		{
			await NormalizeEnemyHpIfNeeded(modifier, enemy);
		}
	}

	public static async Task NormalizeEnemyHpIfNeeded(HextechMayhemModifier modifier, Creature creature)
	{
		if (creature.Side != CombatSide.Enemy || !creature.IsAlive)
		{
			return;
		}

		NetGameType gameType = RunManager.Instance?.NetService?.Type ?? NetGameType.None;
		if (gameType is not (NetGameType.Host or NetGameType.Client))
		{
			return;
		}

		int playerCount = creature.CombatState?.Players.Count ?? modifier.ActiveRunState.Players.Count;
		if (playerCount <= 1)
		{
			return;
		}

		bool localUsesScaling = IsBetterMultiplayerScalingLoaded();
		bool hostUsesScaling = modifier.HostUsesBetterMultiplayerScaling;
		if (hostUsesScaling == localUsesScaling)
		{
			return;
		}

		if (hostUsesScaling)
		{
			await ScaleEnemyHpUpToHostExternalScaling(creature, playerCount);
			if (!_warnedHostOnlyScaling)
			{
				_warnedHostOnlyScaling = true;
				Log.Warn($"[{ModInfo.Id}][Mayhem] BetterMultiplayerScaling is active on the host only; normalized local enemy HP to host scaling.");
			}

			return;
		}

		await ScaleEnemyHpDownFromLocalExternalScaling(creature, playerCount);
		if (!_warnedClientExtraScaling)
		{
			_warnedClientExtraScaling = true;
			Log.Warn($"[{ModInfo.Id}][Mayhem] BetterMultiplayerScaling is active locally but not on the host; normalized local enemy HP down to host scaling.");
		}
	}

	private static async Task ScaleEnemyHpUpToHostExternalScaling(Creature creature, int playerCount)
	{
		int baseMaxHp = GetBaseMonsterMaxHp(creature, playerCount, currentlyScaled: false);
		int expectedMaxHp = Math.Max(1, baseMaxHp * Math.Clamp(playerCount, 1, 16));
		int missingMaxHp = expectedMaxHp - creature.MaxHp;
		if (missingMaxHp > 0)
		{
			await CreatureCmd.GainMaxHp(creature, missingMaxHp);
		}
	}

	private static async Task ScaleEnemyHpDownFromLocalExternalScaling(Creature creature, int playerCount)
	{
		int expectedMaxHp = GetBaseMonsterMaxHp(creature, playerCount, currentlyScaled: true);
		if (expectedMaxHp <= 0 || creature.MaxHp <= expectedMaxHp)
		{
			return;
		}

		int lostHp = Math.Max(0, creature.MaxHp - creature.CurrentHp);
		await CreatureCmdCompat.SetMaxHp(creature, expectedMaxHp);
		await CreatureCmd.SetCurrentHp(creature, Math.Max(0, expectedMaxHp - lostHp));
	}

	private static int GetBaseMonsterMaxHp(Creature creature, int playerCount, bool currentlyScaled)
	{
		if (creature.MonsterMaxHpBeforeModification is int baseMaxHp && baseMaxHp > 0)
		{
			return baseMaxHp;
		}

		if (currentlyScaled && playerCount > 1 && creature.MaxHp % playerCount == 0)
		{
			return Math.Max(1, creature.MaxHp / playerCount);
		}

		return Math.Max(1, creature.MaxHp);
	}
}

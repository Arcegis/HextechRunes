using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechArtifactCompatibilityHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(
				typeof(ArtifactPower),
				nameof(ArtifactPower.TryModifyPowerAmountReceived),
				BindingFlags.Public | BindingFlags.Instance,
				typeof(PowerModel),
				typeof(Creature),
				typeof(decimal),
				typeof(Creature),
				typeof(decimal).MakeByRefType()),
			prefix: new HarmonyMethod(typeof(HextechArtifactCompatibilityHooks), nameof(ArtifactPowerTryModifyPowerAmountReceivedPrefix)));
	}

	private static bool ArtifactPowerTryModifyPowerAmountReceivedPrefix(
		PowerModel canonicalPower,
		decimal amount,
		ref decimal modifiedAmount,
		ref bool __result)
	{
		if (!IsEncounterMechanicPower(canonicalPower))
		{
			return true;
		}

		modifiedAmount = amount;
		__result = false;
		return false;
	}

	private static bool IsEncounterMechanicPower(PowerModel power)
	{
		return power is SurroundedPower or FlankingPower;
	}
}

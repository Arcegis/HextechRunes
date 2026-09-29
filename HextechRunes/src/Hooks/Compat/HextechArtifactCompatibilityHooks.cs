namespace HextechRunes;

internal static class HextechArtifactCompatibilityHooks
{
	private static bool IsEncounterMechanicPower(PowerModel power)
	{
		return power is SurroundedPower or FlankingPower;
	}

	[HarmonyPatch(typeof(ArtifactPower), nameof(ArtifactPower.TryModifyPowerAmountReceived), new[] { typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(decimal) }, new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
	[HextechPatch("compat.artifact", "人工制品遭遇战兼容")]
	private static class TryModifyPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(
			ArtifactPower __instance,
			PowerModel canonicalPower,
			decimal amount,
			ref decimal modifiedAmount,
			ref bool __result)
		{
			if (!IsEncounterMechanicPower(canonicalPower)
				|| !HextechCombatHooks.IsVanillaFixActiveFor(__instance.Owner))
			{
				return true;
			}

			modifiedAmount = amount;
			__result = false;
			return false;
		}
	}
}

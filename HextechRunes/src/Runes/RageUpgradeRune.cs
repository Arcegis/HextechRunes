namespace HextechRunes;

public sealed class RageUpgradeRune : CardUpgradeRuneBase<Rage>
{
	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);

	[HarmonyPatch(typeof(RagePower), nameof(RagePower.AfterSideTurnEnd))]
	[HextechPatch("rune.rage.persistent", "升级狂怒", Rune = typeof(RageUpgradeRune))]
	private static class PersistentRagePatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		internal static bool Prefix(RagePower __instance, ref Task __result)
		{
			if (__instance.Owner.Player?.GetRelic<RageUpgradeRune>() == null) return true;
			__result = Task.CompletedTask;
			return false;
		}
	}
}

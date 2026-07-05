using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechEncounterCompatibilityHooks
{
	private const string EntomancerCastSfx = "event:/sfx/enemy/enemy_attacks/entomancer/entomancer_cast";

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(
				typeof(Entomancer),
				"SpitMove",
				BindingFlags.Instance | BindingFlags.NonPublic,
				typeof(IReadOnlyList<Creature>)),
			prefix: new HarmonyMethod(typeof(HextechEncounterCompatibilityHooks), nameof(EntomancerSpitMovePrefix)));
	}

	private static bool EntomancerSpitMovePrefix(Entomancer __instance, ref Task __result)
	{
		__result = EntomancerSpitMoveSafe(__instance);
		return false;
	}

	private static async Task EntomancerSpitMoveSafe(Entomancer entomancer)
	{
		SfxCmd.Play(EntomancerCastSfx);
		await CreatureCmd.TriggerAnim(entomancer.Creature, "Cast", 0.5f);

		PersonalHivePower? personalHivePower = entomancer.Creature.Powers.OfType<PersonalHivePower>().FirstOrDefault();
		if (personalHivePower == null || personalHivePower.Amount < 3)
		{
			await PowerCmd.Apply<PersonalHivePower>(entomancer.Creature, 1m, entomancer.Creature, null);
			await PowerCmd.Apply<StrengthPower>(entomancer.Creature, 1m, entomancer.Creature, null);
			return;
		}

		await PowerCmd.Apply<StrengthPower>(entomancer.Creature, 2m, entomancer.Creature, null);
	}
}

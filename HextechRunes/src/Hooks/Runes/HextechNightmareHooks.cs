using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

// 梦魇 —— 黑暗充能球(DarkOrb)触发被动后,对生命值最低的存活敌人造成等同于该球当前计数(EvokeVal)的伤害。
internal static class HextechNightmareHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(DarkOrb), nameof(DarkOrb.BeforeTurnEndOrbTrigger), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext)),
			prefix: new HarmonyMethod(typeof(HextechNightmareHooks), nameof(BeforeTurnEndOrbTriggerPrefix)));
		HextechLog.Info($"[{ModInfo.Id}][Nightmare] DarkOrb passive hook installed.");
	}

	private static bool BeforeTurnEndOrbTriggerPrefix(DarkOrb __instance, PlayerChoiceContext choiceContext, ref Task __result)
	{
		Player? player = __instance.Owner;
		if (player?.GetRelic<NightmareRune>() != null)
		{
			__result = TriggerWithNightmare(__instance, choiceContext, player);
			return false;
		}

		return true;
	}

	private static async Task TriggerWithNightmare(DarkOrb orb, PlayerChoiceContext choiceContext, Player player)
	{
		await orb.Passive(choiceContext, null);

		if (player.Creature.IsDead || player.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = HextechCombatCreatureHelper.GetAliveEnemies(combatState);
		if (enemies.Count == 0)
		{
			return;
		}

		Creature weakest = enemies.MinBy(static creature => creature.CurrentHp)!;
		await CreatureCmd.Damage(choiceContext, weakest, orb.EvokeVal, ValueProp.Unpowered, player.Creature);
	}
}

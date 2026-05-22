using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static void InstallRuneSpecificHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), "get_Tags", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(CardTagsPostfix)));

		harmony.Patch(
			RequireMethod(typeof(OrbCmd), nameof(OrbCmd.AddSlots), BindingFlags.Static | BindingFlags.Public, typeof(Player), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(OrbAddSlotsPrefix)));

		InstallElectrodynamicsLightningHook(harmony);
	}

	private static void InstallElectrodynamicsLightningHook(Harmony harmony)
	{
		MethodInfo? lightningApplyDamage = TryGetMethod(
			typeof(LightningOrb),
			"ApplyLightningDamage",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			typeof(decimal),
			typeof(Creature),
			typeof(PlayerChoiceContext));
		if (lightningApplyDamage == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Electrodynamics lightning hook skipped: LightningOrb.ApplyLightningDamage was not found in this game build.");
			return;
		}

		harmony.Patch(
			lightningApplyDamage,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(LightningApplyDamagePrefix)));
	}

	private static void CardTagsPostfix(CardModel __instance, ref IEnumerable<CardTag> __result)
	{
		if (__instance.Type != CardType.Attack
			|| __result.Contains(CardTag.Strike))
		{
			return;
		}

		Player? owner = TryGetMutableCardOwner(__instance);
		if (owner?.GetRelic<DeviantCognitionRune>() == null)
		{
			return;
		}

		__result = __result.Append(CardTag.Strike);
	}

	private static Player? TryGetMutableCardOwner(CardModel card)
	{
		try
		{
			return card.Owner;
		}
		catch (CanonicalModelException)
		{
			return null;
		}
	}

	private static bool OrbAddSlotsPrefix(Player player, int amount, ref Task __result)
	{
		if (player.GetRelic<MadScientistRune>() == null)
		{
			return true;
		}

		if (CombatManager.Instance.IsOverOrEnding || amount <= 0)
		{
			__result = Task.CompletedTask;
			return false;
		}

		if (player.PlayerCombatState == null)
		{
			return true;
		}

		player.PlayerCombatState.OrbQueue.AddCapacity(amount);
		NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddSlotAnim(amount);
		__result = Task.CompletedTask;
		return false;
	}

	private static bool LightningApplyDamagePrefix(LightningOrb __instance, decimal value, Creature? target, PlayerChoiceContext choiceContext, ref Task<IEnumerable<Creature>> __result)
	{
		if (__instance.Owner?.GetRelic<ElectrodynamicsRune>() == null)
		{
			return true;
		}

		__result = ApplyElectrodynamicsLightningDamage(__instance, value, choiceContext);
		return false;
	}

	private static async Task<IEnumerable<Creature>> ApplyElectrodynamicsLightningDamage(LightningOrb orb, decimal value, PlayerChoiceContext choiceContext)
	{
		List<Creature> targets = orb.CombatState.GetOpponentsOf(orb.Owner.Creature)
			.Where(static enemy => enemy.IsHittable)
			.ToList();
		if (targets.Count == 0)
		{
			return Array.Empty<Creature>();
		}

		foreach (Creature target in targets)
		{
			VfxCmd.PlayOnCreature(target, "vfx/vfx_attack_lightning");
		}

		await CreatureCmd.Damage(choiceContext, targets, value, ValueProp.Unpowered, orb.Owner.Creature);
		return targets;
	}
}

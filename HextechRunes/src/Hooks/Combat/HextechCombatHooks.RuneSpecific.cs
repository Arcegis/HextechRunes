using System.Reflection;
using Godot;
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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private const int OrbLayoutRadiusSoftCapSlots = 10;
	private const float OrbLayoutRangeDegrees = 125f;
	private const float OrbLayoutAngleOffsetDegrees = -25f;
	private const float OrbLayoutMaxRadius = 300f;
	private const float OrbLayoutTweenSpeed = 0.45f;

	private static FieldInfo? OrbManagerOrbsField;
	private static FieldInfo? OrbManagerCreatureField;
	private static FieldInfo? OrbManagerCurrentTweenField;

	private static void InstallRuneSpecificHooks(Harmony harmony)
	{
		TryInstallRuneHook<DeviantCognitionRune>("deviant cognition card tags", () => InstallDeviantCognitionHooks(harmony));
		TryInstallRuneHook<IllusoryWeaponRune>("illusory weapon card type", () => InstallIllusoryWeaponHooks(harmony));
		TryInstallRuneHook<FlyingKickRune>("flying kick dynamic description", () => InstallFlyingKickDescriptionHooks(harmony));
		TryInstallCombatHookGroup("flying kick corpse launch visual", () => InstallFlyingKickCorpseLaunchHooks(harmony));
		TryInstallRuneHook<MadScientistRune>("mad scientist orb slots", () => InstallMadScientistHooks(harmony));
		TryInstallCombatHookGroup("orb layout soft cap", () => InstallOrbLayoutSoftCapHooks(harmony));
		TryInstallRuneHook<ElectrodynamicsRune>("electrodynamics lightning", () => InstallElectrodynamicsLightningHook(harmony));
		TryInstallRuneHook<SurvivorUpgradeRune>("survivor upgraded play", () => InstallSurvivorUpgradeHooks(harmony));
		TryInstallRuneHook<CompactUpgradeRune>("compact upgraded play", () => InstallCompactUpgradeHooks(harmony));
		TryInstallRuneHook<WhirlwindUpgradeRune>("whirlwind upgraded x value", () => InstallWhirlwindUpgradeHooks(harmony));
	}

	private static void InstallDeviantCognitionHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), "get_Tags", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(CardTagsPostfix)));
	}

	private static void InstallIllusoryWeaponHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), "get_Type", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(CardTypePostfix)));
	}

	private static void InstallFlyingKickDescriptionHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireGetter(typeof(RelicModel), nameof(RelicModel.DynamicDescription)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(RelicDynamicDescriptionPrefix)));
	}

	private static void InstallFlyingKickCorpseLaunchHooks(Harmony harmony)
	{
		if (HextechRuntimeRuneCompatibility.IsAndroidRuntime)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem][Compat] Flying Kick corpse launch visual hook skipped on Android runtime.");
			return;
		}

		harmony.Patch(
			RequireMethod(typeof(NCreature), nameof(NCreature.StartDeathAnim), BindingFlags.Instance | BindingFlags.Public, typeof(bool)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NCreatureStartDeathAnimPostfix)));
	}

	private static void InstallMadScientistHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(OrbCmd), nameof(OrbCmd.AddSlots), BindingFlags.Static | BindingFlags.Public, typeof(Player), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(OrbAddSlotsPrefix)));
	}

	private static void InstallOrbLayoutSoftCapHooks(Harmony harmony)
	{
		EnsureOrbLayoutFields();
		harmony.Patch(
			RequireMethod(typeof(NOrbManager), "TweenLayout", BindingFlags.Instance | BindingFlags.NonPublic),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(OrbTweenLayoutPrefix)));
	}

	private static void EnsureOrbLayoutFields()
	{
		OrbManagerOrbsField ??= RequireField(typeof(NOrbManager), "_orbs");
		OrbManagerCreatureField ??= RequireField(typeof(NOrbManager), "_creatureNode");
		OrbManagerCurrentTweenField ??= RequireField(typeof(NOrbManager), "_curTween");
	}

	private static void RelicDynamicDescriptionPrefix(RelicModel __instance)
	{
		if (__instance is FlyingKickRune flyingKickRune)
		{
			flyingKickRune.RefreshExecutePercentFromOwner();
		}
	}

	private static void NCreatureStartDeathAnimPostfix(NCreature __instance, bool shouldRemove)
	{
		if (!FlyingKickCorpseLaunchDriver.TryConsumePending(__instance.Entity))
		{
			return;
		}

		if (!shouldRemove
			|| __instance.Entity == null
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(__instance.Entity))
		{
			return;
		}

		FlyingKickCorpseLaunchDriver.TryAttach(__instance);
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
			throw new InvalidOperationException("LightningOrb.ApplyLightningDamage was not found in this game build.");
		}

		harmony.Patch(
			lightningApplyDamage,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(LightningApplyDamagePrefix)));
	}

	private static void InstallSurvivorUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Survivor), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(SurvivorOnPlayPrefix)));
	}

	private static void InstallCompactUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Compact), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(CompactOnPlayPrefix)));
	}

	private static void InstallWhirlwindUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(CardResolveEnergyXValuePostfix)));
	}

	private static bool SurvivorOnPlayPrefix(Survivor __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
	{
		if (!SurvivorUpgradeRune.ShouldUseUpgradedPlay(__instance))
		{
			return true;
		}

		__result = SurvivorUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
		return false;
	}

	private static bool CompactOnPlayPrefix(Compact __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
	{
		if (!CompactUpgradeRune.ShouldUseUpgradedPlay(__instance))
		{
			return true;
		}

		__result = CompactUpgradeRune.PlayUpgraded(choiceContext, __instance, cardPlay);
		return false;
	}

	private static void CardResolveEnergyXValuePostfix(CardModel __instance, ref int __result)
	{
		WhirlwindUpgradeRune.TryDoubleResolvedX(__instance, ref __result);
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

	private static void CardTypePostfix(CardModel __instance, ref CardType __result)
	{
		if (__result != CardType.Skill)
		{
			return;
		}

		Player? owner = TryGetMutableCardOwner(__instance);
		if (IllusoryWeaponRune.ShouldTreatSkillAsAttack(owner))
		{
			__result = CardType.Attack;
		}
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

	private static bool OrbTweenLayoutPrefix(NOrbManager __instance)
	{
		if (!TryGetOrbLayoutState(__instance, out List<NOrb> orbs, out int capacity)
			|| capacity <= OrbLayoutRadiusSoftCapSlots)
		{
			return true;
		}

		if (orbs.Count == 0)
		{
			return false;
		}

		float angle = OrbLayoutRangeDegrees;
		float angleStep = OrbLayoutRangeDegrees / Math.Max(1, capacity - 1);
		float radius = OrbLayoutMaxRadius;
		if (!__instance.IsLocal)
		{
			radius *= 0.75f;
		}

		((Tween?)OrbManagerCurrentTweenField?.GetValue(__instance))?.Kill();
		Tween tween = __instance.CreateTween().SetParallel();
		OrbManagerCurrentTweenField?.SetValue(__instance, tween);

		int layoutCount = Math.Min(capacity, orbs.Count);
		for (int i = 0; i < layoutCount; i++)
		{
			float radians = (OrbLayoutAngleOffsetDegrees - angle) * MathF.PI / 180f;
			Vector2 position = new(-MathF.Cos(radians) * radius, MathF.Sin(radians) * radius);
			tween.TweenProperty(orbs[i], "position", position, OrbLayoutTweenSpeed)
				.SetEase(Tween.EaseType.InOut)
				.SetTrans(Tween.TransitionType.Sine);
			angle -= angleStep;
		}

		return false;
	}

	private static bool TryGetOrbLayoutState(NOrbManager manager, out List<NOrb> orbs, out int capacity)
	{
		orbs = (List<NOrb>?)OrbManagerOrbsField?.GetValue(manager) ?? new List<NOrb>();
		NCreature? creature = (NCreature?)OrbManagerCreatureField?.GetValue(manager);
		capacity = creature?.Entity.Player?.PlayerCombatState?.OrbQueue.Capacity ?? 0;
		return capacity > 0;
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

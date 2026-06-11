using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{
	internal static void Install(Harmony harmony)
	{
		InstallCardIdentityRuneHooks(harmony);
		InstallFlyingKickRuneHooks(harmony);
		InstallDefectOrbRuneHooks(harmony);
		InstallUpgradeRuneHooks(harmony);
	}

	private static void InstallCardIdentityRuneHooks(Harmony harmony)
	{
		TryInstallSharedCardTagHooks(harmony);
		TryInstallRuneHook<BigKnifeRune>("big knife generated shiv replacement", () => InstallBigKnifeHooks(harmony));
		TryInstallRuneHook<IllusoryWeaponRune>("illusory weapon attack counters", () => InstallIllusoryWeaponHooks(harmony));
	}

	private static void InstallFlyingKickRuneHooks(Harmony harmony)
	{
		TryInstallRuneHook<FlyingKickRune>("flying kick dynamic description", () => InstallFlyingKickDescriptionHooks(harmony));
		TryInstallCombatHookGroup("flying kick corpse launch visual", () => InstallFlyingKickCorpseLaunchHooks(harmony));
	}

	private static void InstallDefectOrbRuneHooks(Harmony harmony)
	{
		TryInstallRuneHook<MadScientistRune>("mad scientist orb slots", () => InstallMadScientistHooks(harmony));
		TryInstallCombatHookGroup("orb layout soft cap", () => InstallOrbLayoutSoftCapHooks(harmony));
		TryInstallRuneHook<ElectrodynamicsRune>("electrodynamics lightning", () => InstallElectrodynamicsLightningHook(harmony));
	}

	private static void InstallUpgradeRuneHooks(Harmony harmony)
	{
		TryInstallRuneHook<SurvivorUpgradeRune>("survivor upgraded play", () => InstallSurvivorUpgradeHooks(harmony));
		TryInstallRuneHook<CompactUpgradeRune>("compact upgraded play", () => InstallCompactUpgradeHooks(harmony));
		TryInstallRuneHook<WhirlwindUpgradeRune>("whirlwind upgraded x value", () => InstallWhirlwindUpgradeHooks(harmony));
		TryInstallRuneHook<JuggernautUpgradeRune>("juggernaut upgraded block damage", () => InstallJuggernautUpgradeHooks(harmony));
		TryInstallRuneHook<HiddenGemUpgradeRune>("hidden gem upgraded play", () => InstallHiddenGemUpgradeHooks(harmony));
		TryInstallRuneHook<AutomationUpgradeRune>("automation upgraded draw", () => InstallAutomationUpgradeHooks(harmony));
		TryInstallRuneHook<VoltaicUpgradeRune>("voltaic upgraded play", () => InstallVoltaicUpgradeHooks(harmony));
		TryInstallRuneHook<GrandFinaleUpgradeRune>("grand finale upgraded play", () => InstallGrandFinaleUpgradeHooks(harmony));
		TryInstallRuneHook<VoidFormUpgradeRune>("void form upgraded play", () => InstallVoidFormUpgradeHooks(harmony));
		TryInstallRuneHook<RainbowUpgradeRune>("rainbow upgraded play", () => InstallRainbowUpgradeHooks(harmony));
	}

	private static void TryInstallSharedCardTagHooks(Harmony harmony)
	{
		try
		{
			InstallCardTagHooks(harmony);
		}
		catch (Exception ex)
		{
			HextechRuntimeRuneCompatibility.MarkPlayerRuneHookFailed<DeviantCognitionRune>("card tags", ex);
			HextechRuntimeRuneCompatibility.MarkPlayerRuneHookFailed<BigKnifeRune>("card tags", ex);
		}
	}

	private static void InstallCardTagHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), "get_Tags", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(CardTagsPostfix)));
	}

	private static void InstallBigKnifeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Shiv), nameof(Shiv.CreateInHand), BindingFlags.Public | BindingFlags.Static, typeof(Player), typeof(CombatState)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(ShivCreateOneInHandPrefix)));
		harmony.Patch(
			RequireMethod(typeof(Shiv), nameof(Shiv.CreateInHand), BindingFlags.Public | BindingFlags.Static, typeof(Player), typeof(int), typeof(CombatState)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(ShivCreateManyInHandPrefix)));
		harmony.Patch(
			RequireMethod(typeof(SovereignBlade), "get_TargetType", BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(SovereignBladeTargetTypePostfix)));
		harmony.Patch(
			RequireMethod(typeof(SovereignBlade), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(SovereignBladeOnPlayPrefix)));
#if STS2_104_OR_NEWER
		harmony.Patch(
			RequireMethod(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat), BindingFlags.Public | BindingFlags.Static, typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(CardPileCmdAddGeneratedCardsToCombatPrefix)));
#else
		harmony.Patch(
			RequireMethod(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat), BindingFlags.Public | BindingFlags.Static, typeof(IEnumerable<CardModel>), typeof(PileType), typeof(bool), typeof(CardPilePosition)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(CardPileCmdAddGeneratedCardsToCombatPrefix)));
#endif
	}

	private static void InstallFlyingKickDescriptionHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireGetter(typeof(RelicModel), nameof(RelicModel.DynamicDescription)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(RelicDynamicDescriptionPrefix)));
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
			postfix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(NCreatureStartDeathAnimPostfix)));
	}

	private static void InstallMadScientistHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(OrbCmd), nameof(OrbCmd.AddSlots), BindingFlags.Static | BindingFlags.Public, typeof(Player), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(OrbAddSlotsPrefix)));
	}

	private static void InstallOrbLayoutSoftCapHooks(Harmony harmony)
	{
		EnsureOrbLayoutFields();
		harmony.Patch(
			RequireMethod(typeof(NOrbManager), "TweenLayout", BindingFlags.Instance | BindingFlags.NonPublic),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(OrbTweenLayoutPrefix)));
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
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(LightningApplyDamagePrefix)));
	}

	private static void InstallSurvivorUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Survivor), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(SurvivorOnPlayPrefix)));
	}

	private static void InstallCompactUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Compact), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(CompactOnPlayPrefix)));
	}

	private static void InstallWhirlwindUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.ResolveEnergyXValue), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(CardResolveEnergyXValuePostfix)));
	}

	private static void InstallJuggernautUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(JuggernautPower), nameof(JuggernautPower.AfterBlockGained), BindingFlags.Instance | BindingFlags.Public, typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(JuggernautAfterBlockGainedPrefix)));
	}

	private static void InstallHiddenGemUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(HiddenGem), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(HiddenGemOnPlayPrefix)));
	}

	private static void InstallAutomationUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(AutomationPower), nameof(AutomationPower.AfterCardDrawn), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(AutomationAfterCardDrawnPrefix)));
	}

	private static void InstallVoltaicUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Voltaic), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(VoltaicOnPlayPrefix)));
	}

	private static void InstallGrandFinaleUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(GrandFinale), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(GrandFinaleOnPlayPrefix)));
	}

	private static void InstallVoidFormUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(VoidForm), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(VoidFormOnPlayPrefix)));
	}

	private static void InstallRainbowUpgradeHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Rainbow), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PlayerChoiceContext), typeof(CardPlay)),
			prefix: new HarmonyMethod(typeof(HextechPlayerRuneHooks), nameof(RainbowOnPlayPrefix)));
	}

	private static void TryInstallCombatHookGroup(string label, Action install)
	{
		try
		{
			install();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Combat hook group skipped: {label}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void TryInstallRuneHook<TRune>(string label, Action install)
		where TRune : RelicModel
	{
		try
		{
			install();
		}
		catch (Exception ex)
		{
			HextechRuntimeRuneCompatibility.MarkPlayerRuneHookFailed<TRune>(label, ex);
		}
	}
}

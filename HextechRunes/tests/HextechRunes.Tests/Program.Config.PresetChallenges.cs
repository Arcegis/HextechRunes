using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using HextechRunes;
using FormVfxKind = HextechRunes.HextechFormVfxSafetyHooks.FormVfxKind;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void PresetChallengesScheduleThreeEnabledActPlans()
	{
		IReadOnlySet<MonsterHexKind> enabledHexes = HextechContentRegistry.MonsterHexMetadata.EnabledKindsByRarity.Values
			.SelectMany(static kinds => kinds)
			.ToHashSet();
		foreach (Type challengeType in HextechCustomModelRegistry.CustomChallengeModifierTypes)
		{
			Expect(HextechPresetChallengeRegistry.IsChallengeModifierType(challengeType), $"{challengeType.Name} has act plans");
			Expect(
				HextechCustomModelRegistry.AllCustomModifierTypes.Contains(challengeType),
				$"{challengeType.Name} should be included in saved-property model registration");
			for (int actIndex = 0; actIndex < 3; actIndex++)
			{
				Expect(
					HextechPresetChallengeRegistry.TryGetActPlan(challengeType, actIndex, out HextechPresetChallengeActPlan? plan),
					$"{challengeType.Name} act {actIndex + 1} should exist");
				Expect(plan.EnemyHexes.Count > 0, $"{challengeType.Name} act {actIndex + 1} schedules enemy hexes");
				foreach (MonsterHexKind hex in plan.EnemyHexes)
				{
					Expect(enabledHexes.Contains(hex), $"{challengeType.Name} act {actIndex + 1} enemy hex {hex} is registered and enabled");
				}
			}

			Expect(
				!HextechPresetChallengeRegistry.TryGetActPlan(challengeType, 3, out _),
				$"{challengeType.Name} should not schedule a fourth acquisition");
		}

		HextechRunConfigurationSnapshot defaultSnapshot = HextechRuneConfiguration.GetDefaultSnapshot();
		SequenceEqual(new[] { 1, 1, 1 }, defaultSnapshot.PlayerHexCountsByAct, "challenge default player counts");
		SequenceEqual(new[] { 1, 2, 3 }, defaultSnapshot.EnemyHexCountsByAct, "challenge default enemy counts");
		Expect(
			defaultSnapshot.RuneRarityWeightsByAct.All(static weights => weights == new HextechRarityWeights(1, 1, 1)),
			"challenge default rarity weights should be 1:1:1 in every act");

		foreach (Type selectedType in HextechCustomModelRegistry.CustomChallengeModifierTypes)
		{
			foreach (Type candidateType in HextechCustomModelRegistry.CustomChallengeModifierTypes)
			{
				Equal(
					selectedType != candidateType,
					HextechPresetChallengeRegistry.AreMutuallyExclusiveChallengeTypes(selectedType, candidateType),
					$"challenge exclusivity {selectedType.Name} -> {candidateType.Name}");
			}
		}

		Expect(
			!HextechPresetChallengeRegistry.AreMutuallyExclusiveChallengeTypes(
				typeof(StuffedToRuinChallengeModifier),
				typeof(HextechSilverRunModifier)),
			"preset challenges should not untick ordinary custom-run modifiers");
	}
}

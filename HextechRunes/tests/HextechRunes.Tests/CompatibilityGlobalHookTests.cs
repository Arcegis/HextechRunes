using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes.Tests;

internal static partial class Program
{
	private static void ArtifactCompatibilityPreservesCardDebuffsAndEncounterException()
	{
		Creature target = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
		ArtifactPower artifact = CreateMutableTestModel<ArtifactPower>();
		AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(artifact, target);
		FlankingPower flanking = new();
		WeakPower weak = new();
		SurroundedPower surrounded = new();

		Expect(artifact.TryModifyPowerAmountReceived(flanking, target, 2m, null, out decimal nativeAmount),
			"vanilla Artifact intercepts the card-applied Flanking debuff");
		Equal(0m, nativeAmount, "vanilla Artifact blocks Flanking");

		Harmony harmony = new("Natsuki.HextechRunes.Tests.ArtifactCompatibility");
		try
		{
			HextechPatcher.ApplyNested(harmony, typeof(HextechArtifactCompatibilityHooks));
			Expect(artifact.TryModifyPowerAmountReceived(flanking, target, 2m, null, out decimal flankingAmount),
				"the compatibility prefix preserves vanilla Flanking interception without a Mayhem run");
			Equal(0m, flankingAmount, "Artifact still blocks Flanking with the compatibility patch installed");
			Expect(artifact.TryModifyPowerAmountReceived(weak, target, 3m, null, out decimal weakAmount),
				"ordinary debuffs still use vanilla Artifact interception");
			Equal(0m, weakAmount, "Artifact still blocks Weak");
			Expect(!artifact.TryModifyPowerAmountReceived(surrounded, target, 1m, null, out decimal surroundedAmount),
				"the existing Surrounded encounter exception remains enabled");
			Equal(1m, surroundedAmount, "the encounter exception preserves Surrounded's amount");
		}
		finally
		{
			harmony.UnpatchAll(harmony.Id);
		}
	}

	private static void ActualDamageHookCannotSuppressOutOfCombatCalls()
	{
		MethodInfo prefix = HextechPatcher.FindPatchMethod(typeof(HextechCombatHooks), "DamageCommandPatch", "Prefix")
			?? throw new InvalidOperationException("Actual damage command prefix is missing.");

		Equal(typeof(void), prefix.ReturnType, "actual damage prefix return type");
		ParameterInfo[] parameters = prefix.GetParameters();
		Equal(1, parameters.Length, "actual damage prefix parameter count");
		Expect(parameters[0].IsOut && parameters[0].ParameterType == typeof(long).MakeByRefType(),
			"Actual damage prefix should only allocate command state and must not receive targets or replace the result.");
	}

	private static void HookReflectionRequiresExactSignatures()
	{
		MethodInfo exact = HextechHookReflection.RequireMethod(
			typeof(ReflectionSignatureFixture),
			nameof(ReflectionSignatureFixture.Target),
			BindingFlags.NonPublic | BindingFlags.Static,
			typeof(string));
		Equal(typeof(string), exact.GetParameters()[0].ParameterType, "exact reflection parameter");

		ExpectThrows<InvalidOperationException>(
			() => HextechHookReflection.RequireMethod(
				typeof(ReflectionSignatureFixture),
				nameof(ReflectionSignatureFixture.Target),
				BindingFlags.NonPublic | BindingFlags.Static,
				typeof(int)),
			"A same-name, same-arity method with a different signature must not be selected.");
	}

	private static class ReflectionSignatureFixture
	{
		internal static void Target(string value)
		{
			_ = value;
		}
	}
}

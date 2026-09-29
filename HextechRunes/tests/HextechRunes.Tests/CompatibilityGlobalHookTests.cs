using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes.Tests;

internal static partial class Program
{
	[HextechTest]
	private static void ArtifactCompatibilityPreservesCardDebuffsAndEncounterException()
	{
		// 只核对补丁自身的判定，不在测试进程里安装真实 Harmony 补丁：对原版方法打补丁在测试宿主上会原生崩溃。
		MethodInfo isEncounterMechanic = typeof(HextechArtifactCompatibilityHooks).GetMethod("IsEncounterMechanicPower", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("Artifact encounter-mechanic predicate is missing.");
		bool IsEncounterMechanic(PowerModel power) => (bool)isEncounterMechanic.Invoke(null, [power])!;

		FlankingPower flanking = new();
		WeakPower weak = new();
		SurroundedPower surrounded = new();
		Expect(!IsEncounterMechanic(flanking), "Flanking is a card-applied debuff that vanilla Artifact must keep blocking");
		Expect(!IsEncounterMechanic(weak), "ordinary debuffs keep vanilla Artifact interception");
		Expect(IsEncounterMechanic(surrounded), "the existing Surrounded encounter exception remains enabled");

		MethodInfo prefix = FindPatchMethod(typeof(HextechArtifactCompatibilityHooks), "TryModifyPatch", "Prefix")
			?? throw new InvalidOperationException("Artifact compatibility prefix is missing.");
		ArtifactPower artifact = CreateMutableTestModel<ArtifactPower>();
		object?[] arguments = [artifact, flanking, 2m, 0m, false];
		Expect((bool)prefix.Invoke(null, arguments)!, "the prefix lets vanilla Artifact intercept Flanking");
		Equal(0m, (decimal)arguments[3]!, "the prefix leaves the modified amount untouched for Flanking");
	}

	[HextechTest]
	private static void ActualDamageHookCannotSuppressOutOfCombatCalls()
	{
		MethodInfo prefix = FindPatchMethod(typeof(HextechCombatHooks), "DamageCommandPatch", "Prefix")
			?? throw new InvalidOperationException("Actual damage command prefix is missing.");

		Equal(typeof(void), prefix.ReturnType, "actual damage prefix return type");
		ParameterInfo[] parameters = prefix.GetParameters();
		Equal(1, parameters.Length, "actual damage prefix parameter count");
		Expect(parameters[0].IsOut && parameters[0].ParameterType == typeof(long).MakeByRefType(),
			"Actual damage prefix should only allocate command state and must not receive targets or replace the result.");
	}

	[HextechTest]
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

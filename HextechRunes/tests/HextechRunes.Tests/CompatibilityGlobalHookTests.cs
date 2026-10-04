using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
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
		(HextechEnemyHexContext _, Player first, Player _) = CreatePrismaticEnemyFixture();
		Creature owner = first.Creature;
		Creature other = CreatePrismaticTestCreature(CombatSide.Enemy, (CombatState)owner.CombatState!);
		AccessTools.Field(typeof(Creature), "_powers").SetValue(owner, new List<PowerModel>());
		ArtifactPower artifact = CreateMutableTestModel<ArtifactPower>();
		AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(artifact, owner);
		(bool RunsOriginal, decimal Modified, bool Blocked) Invoke(PowerModel power, Creature target, decimal amount, Creature? applier)
		{
			object?[] arguments = [artifact, power, target, amount, applier, -99m, false];
			bool runsOriginal = (bool)prefix.Invoke(null, arguments)!;
			return (runsOriginal, (decimal)arguments[5]!, (bool)arguments[6]!);
		}

		(bool runsOriginal, decimal modified, bool _) = Invoke(flanking, owner, 2m, other);
		Expect(runsOriginal, "the prefix lets vanilla Artifact intercept Flanking");
		Equal(-99m, modified, "the prefix leaves the modified amount untouched for Flanking");
		Expect(Invoke(CreateMutableTestModel<HextechTemporaryStrengthLossPower>(), other, 1m, owner).RunsOriginal,
			"another creature's Artifact does not judge powers applied to this owner");

		foreach (PowerModel hiddenLoss in new PowerModel[]
		{
			CreateMutableTestModel<HextechTemporaryStrengthLossPower>(),
			CreateMutableTestModel<HextechTemporaryDexterityLossPower>()
		})
		{
			Expect(!hiddenLoss.IsVisible, $"{hiddenLoss.GetType().Name} stays hidden");
			(bool lossRunsOriginal, decimal lossModified, bool lossBlocked) = Invoke(hiddenLoss, owner, 1m, other);
			Expect(!lossRunsOriginal && lossBlocked && lossModified == 0m,
				$"Artifact blocks the whole hidden {hiddenLoss.GetType().Name} wrapper instead of only its inner stat loss");
		}
		Expect(Invoke(CreateMutableTestModel<HextechTemporaryStrengthPower>(), owner, 1m, owner).RunsOriginal,
			"temporary stat gains keep vanilla handling");
	}

	[HextechTest]
	private static void ArtifactIgnoresTemporaryShrinkTickDown()
	{
		(HextechEnemyHexContext _, Player first, Player _) = CreatePrismaticEnemyFixture();
		Creature owner = first.Creature;
		Creature enemy = CreatePrismaticTestCreature(CombatSide.Enemy, (CombatState)owner.CombatState!);
		List<PowerModel> powers = new();
		AccessTools.Field(typeof(Creature), "_powers").SetValue(owner, powers);
		ShrinkPower temporary = CreateMutableTestModel<ShrinkPower>();
		AccessTools.Field(typeof(PowerModel), "_amount").SetValue(temporary, 2);
		powers.Add(temporary);

		Equal(PowerType.Debuff, temporary.GetTypeForAmount(-1m), "vanilla judges a shrink decrement as a debuff");
		Expect(HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(temporary, owner, -1m, null),
			"the end-of-turn decrement of temporary shrink is not a debuff application");
		Expect(HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(temporary, owner, -1m, owner),
			"self-driven decrement is also a tick-down");
		Expect(!HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(temporary, owner, -1m, enemy),
			"an enemy pushing permanent shrink onto temporary shrink is still a debuff");
		Expect(!HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(CreateMutableTestModel<ShrinkPower>(), owner, -1m, null),
			"applying a new permanent shrink (not yet on the owner) is still a debuff");
		Expect(!HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(temporary, owner, 1m, null),
			"adding temporary shrink is still a debuff");

		AccessTools.Field(typeof(PowerModel), "_amount").SetValue(temporary, -1);
		Expect(!HextechArtifactCompatibilityHooks.IsTemporaryShrinkTickDown(temporary, owner, -1m, null),
			"permanent shrink has no tick-down");
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
}

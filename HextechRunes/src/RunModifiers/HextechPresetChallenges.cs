using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace HextechRunes;

public sealed class StuffedToRuinChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_STUFFED_TO_RUIN_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_STUFFED_TO_RUIN_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/stuffedToRuinChallenge.png";
}

public sealed class DefenseCounterMasterChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_DEFENSE_COUNTER_MASTER_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_DEFENSE_COUNTER_MASTER_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/defenseCounterMasterChallenge.png";
}

public sealed class BruteForceChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_BRUTE_FORCE_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_BRUTE_FORCE_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/bruteForceChallenge.png";
}

public sealed class EightPennyGateChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_EIGHT_PENNY_GATE_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_EIGHT_PENNY_GATE_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/eightPennyGateChallenge.png";
}

public sealed class ListlessChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_LISTLESS_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_LISTLESS_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/listlessChallenge.png";
}

internal sealed record HextechPresetChallengeActPlan(
	HextechRarityTier PlayerRarity,
	IReadOnlyList<MonsterHexKind> EnemyHexes);

internal static class HextechPresetChallengeRegistry
{
	private static readonly IReadOnlyList<HextechPresetChallengeActPlan> StuffedToRuinActs =
	[
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.ForgottenSoul ]),
		new(HextechRarityTier.Gold, [ MonsterHexKind.PhrogParasite, MonsterHexKind.ManipulateReality ]),
		new(HextechRarityTier.Silver, [ MonsterHexKind.LeafSlime, MonsterHexKind.DizzySpinning ])
	];

	private static readonly IReadOnlyList<HextechPresetChallengeActPlan> DefenseCounterMasterActs =
	[
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.Exoskeleton ]),
		new(HextechRarityTier.Gold, [ MonsterHexKind.HundredRefinements, MonsterHexKind.Porcupine ]),
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.ProteinShake, MonsterHexKind.UnmovableMountain ])
	];

	private static readonly IReadOnlyList<HextechPresetChallengeActPlan> BruteForceActs =
	[
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.Goliath ]),
		new(HextechRarityTier.Gold, [ MonsterHexKind.AstralBody, MonsterHexKind.VitalitySurge ]),
		new(HextechRarityTier.Gold, [ MonsterHexKind.StatsOnStats, MonsterHexKind.TankEngine ])
	];

	private static readonly IReadOnlyList<HextechPresetChallengeActPlan> EightPennyGateActs =
	[
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.EightPennyGate ]),
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.IGrip ]),
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.IInspect ])
	];

	private static readonly IReadOnlyList<HextechPresetChallengeActPlan> ListlessActs =
	[
		new(HextechRarityTier.Gold, [ MonsterHexKind.MonarchsGaze ]),
		new(HextechRarityTier.Silver, [ MonsterHexKind.TheLost, MonsterHexKind.TheForgotten ]),
		new(HextechRarityTier.Prismatic, [ MonsterHexKind.LagavulinMatriarch, MonsterHexKind.MasterOfDuality ])
	];

	private static readonly IReadOnlyDictionary<Type, IReadOnlyList<HextechPresetChallengeActPlan>> ActsByModifierType =
		new Dictionary<Type, IReadOnlyList<HextechPresetChallengeActPlan>>
		{
			[typeof(StuffedToRuinChallengeModifier)] = StuffedToRuinActs,
			[typeof(DefenseCounterMasterChallengeModifier)] = DefenseCounterMasterActs,
			[typeof(BruteForceChallengeModifier)] = BruteForceActs,
			[typeof(EightPennyGateChallengeModifier)] = EightPennyGateActs,
			[typeof(ListlessChallengeModifier)] = ListlessActs
		};

	internal static bool IsActive(RunState runState)
	{
		return runState.Modifiers.Any(static modifier => IsChallengeModifierType(modifier.GetType()));
	}

	internal static bool TryGetActPlan(RunState runState, int actIndex, [NotNullWhen(true)] out HextechPresetChallengeActPlan? plan)
	{
		foreach (ModifierModel modifier in runState.Modifiers)
		{
			if (TryGetActPlan(modifier.GetType(), actIndex, out plan))
			{
				return true;
			}
		}

		plan = null;
		return false;
	}

	internal static bool TryGetActPlan(Type modifierType, int actIndex, [NotNullWhen(true)] out HextechPresetChallengeActPlan? plan)
	{
		if (ActsByModifierType.TryGetValue(modifierType, out IReadOnlyList<HextechPresetChallengeActPlan>? acts)
			&& (uint)actIndex < (uint)acts.Count)
		{
			plan = acts[actIndex];
			return true;
		}

		plan = null;
		return false;
	}

	internal static bool IsChallengeModifierType(Type modifierType)
	{
		return ActsByModifierType.ContainsKey(modifierType);
	}

	internal static bool AreMutuallyExclusiveChallengeTypes(Type selectedType, Type candidateType)
	{
		return selectedType != candidateType
			&& IsChallengeModifierType(selectedType)
			&& IsChallengeModifierType(candidateType);
	}
}

internal static class HextechPresetChallengeHooks
{
	private static IEnumerable<ModifierModel> CreatePresetChallenges()
	{
		yield return ModelDb.Modifier<StuffedToRuinChallengeModifier>().ToMutable();
		yield return ModelDb.Modifier<DefenseCounterMasterChallengeModifier>().ToMutable();
		yield return ModelDb.Modifier<BruteForceChallengeModifier>().ToMutable();
		yield return ModelDb.Modifier<EightPennyGateChallengeModifier>().ToMutable();
		yield return ModelDb.Modifier<ListlessChallengeModifier>().ToMutable();
	}

	[HarmonyPatch(typeof(NCustomRunModifiersList), "GetAllModifiers")]
	[HextechPatch("custom-run.preset-challenges.list", "预设挑战", Optional = true)]
	private static class AllModifiersPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<ModifierModel> __result)
		{
			__result = __result.Concat(CreatePresetChallenges());
		}
	}

	[HarmonyPatch(typeof(NCustomRunModifiersList), "UntickMutuallyExclusiveModifiersForTickbox", typeof(NRunModifierTickbox))]
	[HextechPatch("custom-run.preset-challenges.exclusivity", "预设挑战", Optional = true)]
	private static class ExclusivityPatch
	{
		// 原版把勾选框同时存进私有列表 _modifierTickboxes 和容器节点（_Ready 里 AddChildSafely），
		// 这里经公开的节点树取同级勾选框，不注入私有字段。
		[HarmonyPostfix]
		private static void Postfix(NRunModifierTickbox tickbox)
		{
			ModifierModel? selectedModifier = tickbox.Modifier;
			Node? container = tickbox.GetParent();
			if (!tickbox.IsTicked
				|| container == null
				|| selectedModifier == null
				|| !HextechPresetChallengeRegistry.IsChallengeModifierType(selectedModifier.GetType()))
			{
				return;
			}

			foreach (NRunModifierTickbox otherTickbox in container.GetChildren().OfType<NRunModifierTickbox>())
			{
				ModifierModel? otherModifier = otherTickbox.Modifier;
				if (otherModifier != null
					&& HextechPresetChallengeRegistry.AreMutuallyExclusiveChallengeTypes(
						selectedModifier.GetType(),
						otherModifier.GetType()))
				{
					otherTickbox.IsTicked = false;
				}
			}
		}
	}
}

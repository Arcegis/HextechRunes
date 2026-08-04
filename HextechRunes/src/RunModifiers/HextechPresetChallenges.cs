using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

public sealed class StuffedToRuinChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_STUFFED_TO_RUIN_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_STUFFED_TO_RUIN_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/slimedBerserkerHex.png";
}

public sealed class DefenseCounterMasterChallengeModifier : ModifierModel
{
	public override LocString Title => new("modifiers", "HEXTECH_DEFENSE_COUNTER_MASTER_CHALLENGE.title");

	public override LocString Description => new("modifiers", "HEXTECH_DEFENSE_COUNTER_MASTER_CHALLENGE.description");

	protected override string IconPath => $"res://{ModInfo.Id}/images/relics/exoskeletonHex.png";
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

	internal static bool IsActive(RunState runState)
	{
		return runState.Modifiers.Any(static modifier => IsChallengeModifierType(modifier.GetType()));
	}

	internal static bool TryGetActPlan(RunState runState, int actIndex, out HextechPresetChallengeActPlan plan)
	{
		foreach (ModifierModel modifier in runState.Modifiers)
		{
			if (TryGetActPlan(modifier.GetType(), actIndex, out plan))
			{
				return true;
			}
		}

		plan = null!;
		return false;
	}

	internal static bool TryGetActPlan(Type modifierType, int actIndex, out HextechPresetChallengeActPlan plan)
	{
		IReadOnlyList<HextechPresetChallengeActPlan>? acts = modifierType == typeof(StuffedToRuinChallengeModifier)
			? StuffedToRuinActs
			: modifierType == typeof(DefenseCounterMasterChallengeModifier)
				? DefenseCounterMasterActs
				: null;
		if (acts != null && (uint)actIndex < (uint)acts.Count)
		{
			plan = acts[actIndex];
			return true;
		}

		plan = null!;
		return false;
	}

	private static bool IsChallengeModifierType(Type modifierType)
	{
		return modifierType == typeof(StuffedToRuinChallengeModifier)
			|| modifierType == typeof(DefenseCounterMasterChallengeModifier);
	}
}

internal static class HextechPresetChallengeHooks
{
	public static void Install(Harmony harmony)
	{
		MethodInfo? getAllModifiers = TryGetMethod(
			typeof(NCustomRunModifiersList),
			"GetAllModifiers",
			BindingFlags.Instance | BindingFlags.NonPublic,
			warnIfMissing: false);
		if (getAllModifiers == null)
		{
			Log.Warn($"[{ModInfo.Id}][CustomRun] Could not install preset challenge option; NCustomRunModifiersList.GetAllModifiers was not found.");
			return;
		}

		harmony.Patch(
			getAllModifiers,
			postfix: new HarmonyMethod(typeof(HextechPresetChallengeHooks), nameof(AppendPresetChallengesPostfix)));
	}

	private static void AppendPresetChallengesPostfix(ref IEnumerable<ModifierModel> __result)
	{
		__result = __result.Concat(CreatePresetChallenges());
	}

	private static IEnumerable<ModifierModel> CreatePresetChallenges()
	{
		yield return ModelDb.Modifier<StuffedToRuinChallengeModifier>().ToMutable();
		yield return ModelDb.Modifier<DefenseCounterMasterChallengeModifier>().ToMutable();
	}
}

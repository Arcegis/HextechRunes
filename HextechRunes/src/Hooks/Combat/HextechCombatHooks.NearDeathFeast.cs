using System.Reflection;
using MegaCrit.Sts2.addons.mega_text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static readonly FieldInfo CreatureCurrentHpField = RequireField(typeof(Creature), "_currentHp");
	private static readonly FieldInfo CreatureCurrentHpChangedField = RequireField(typeof(Creature), "CurrentHpChanged");
	private static readonly FieldInfo HealthBarCreatureField = RequireField(typeof(NHealthBar), "_creature");
	private static readonly FieldInfo HealthBarHpLabelField = RequireField(typeof(NHealthBar), "_hpLabel");

	private static void InstallNearDeathFeastHooks(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Creature), nameof(Creature.LoseHpInternal), BindingFlags.Instance | BindingFlags.Public, typeof(decimal), typeof(ValueProp)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastLoseHpInternalPrefix)));
		harmony.Patch(
			RequireMethod(typeof(Creature), "set_CurrentHp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastCurrentHpSetterPrefix)));
		harmony.Patch(
			RequireGetter(typeof(Creature), nameof(Creature.IsAlive)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastIsAlivePostfix)));
		harmony.Patch(
			RequireGetter(typeof(Creature), nameof(Creature.IsDead)),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastIsDeadPostfix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastGainBlockPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(BlockVar), typeof(CardPlay), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastGainBlockVarPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.Kill), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.Kill), BindingFlags.Static | BindingFlags.Public, typeof(IReadOnlyCollection<Creature>), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillManyPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CreatureCmd), "KillWithoutCheckingWinCondition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, typeof(Creature), typeof(bool), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillPrefix)));
		harmony.Patch(
			RequireMethod(typeof(NHealthBar), "RefreshText", BindingFlags.Instance | BindingFlags.NonPublic),
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastHealthBarRefreshTextPostfix)));
	}

	private static bool NearDeathFeastLoseHpInternalPrefix(Creature __instance, decimal amount, ValueProp props, ref DamageResult __result)
	{
		if (!NearDeathFeastRune.HasDyingState(__instance))
		{
			return true;
		}

		__result = NearDeathFeastRune.LoseHpAllowingDying(__instance, amount, props);
		return false;
	}

	private static bool NearDeathFeastCurrentHpSetterPrefix(Creature __instance, int value)
	{
		if (value >= 0 || !NearDeathFeastRune.HasDyingState(__instance))
		{
			return true;
		}

		int oldHp = __instance.CurrentHp;
		if (oldHp == value)
		{
			return false;
		}

		CreatureCurrentHpField.SetValue(__instance, value);
		((Action<int, int>?)CreatureCurrentHpChangedField.GetValue(__instance))?.Invoke(oldHp, value);
		return false;
	}

	private static void NearDeathFeastIsAlivePostfix(Creature __instance, ref bool __result)
	{
		if (!__result && NearDeathFeastRune.IsDyingButAlive(__instance))
		{
			__result = true;
		}
	}

	private static void NearDeathFeastIsDeadPostfix(Creature __instance, ref bool __result)
	{
		if (__result && NearDeathFeastRune.IsDyingButAlive(__instance))
		{
			__result = false;
		}
	}

	private static bool NearDeathFeastGainBlockPrefix(Creature creature, ref Task<decimal> __result)
	{
		if (!NearDeathFeastRune.ShouldPreventSustain(creature))
		{
			return true;
		}

		__result = Task.FromResult(0m);
		return false;
	}

	private static bool NearDeathFeastGainBlockVarPrefix(Creature creature, ref Task<decimal> __result)
	{
		return NearDeathFeastGainBlockPrefix(creature, ref __result);
	}

	private static void NearDeathFeastKillPrefix(Creature creature)
	{
		NearDeathFeastRune.ForceDeathThresholdForKill(creature);
	}

	private static void NearDeathFeastKillManyPrefix(IReadOnlyCollection<Creature> creatures)
	{
		foreach (Creature creature in creatures)
		{
			NearDeathFeastRune.ForceDeathThresholdForKill(creature);
		}
	}

	private static void NearDeathFeastHealthBarRefreshTextPostfix(NHealthBar __instance)
	{
		if (HealthBarCreatureField.GetValue(__instance) is not Creature creature
			|| !NearDeathFeastRune.IsDyingButAlive(creature)
			|| HealthBarHpLabelField.GetValue(__instance) is not MegaLabel hpLabel)
		{
			return;
		}

		hpLabel.SetTextAutoSize($"{creature.CurrentHp}/{creature.MaxHp}");
	}
}

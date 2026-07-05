using MegaCrit.Sts2.addons.mega_text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static FieldInfo? HealthBarCreatureField;
	private static FieldInfo? HealthBarHpLabelField;

	private static void InstallNearDeathFeastHooks(Harmony harmony)
	{
		EnsureNearDeathFeastFields();
		MethodInfo loseHpInternal = RequireMethod(typeof(Creature), nameof(Creature.LoseHpInternal), BindingFlags.Instance | BindingFlags.Public, typeof(decimal), typeof(ValueProp));
		MethodInfo currentHpSetter = RequireMethod(typeof(Creature), "set_CurrentHp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, typeof(int));
		MethodInfo isAliveGetter = RequireGetter(typeof(Creature), nameof(Creature.IsAlive));
		MethodInfo isDeadGetter = RequireGetter(typeof(Creature), nameof(Creature.IsDead));
		MethodInfo gainBlock = RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool));
		MethodInfo gainBlockVar = RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(BlockVar), typeof(CardPlay), typeof(bool));
		MethodInfo kill = RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.Kill), BindingFlags.Static | BindingFlags.Public, typeof(Creature), typeof(bool));
		MethodInfo killMany = RequireMethod(typeof(CreatureCmd), nameof(CreatureCmd.Kill), BindingFlags.Static | BindingFlags.Public, typeof(IReadOnlyCollection<Creature>), typeof(bool));
		MethodInfo killWithoutCheckingWinCondition = RequireMethod(typeof(CreatureCmd), "KillWithoutCheckingWinCondition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, typeof(Creature), typeof(bool), typeof(int));
		MethodInfo healthBarRefreshText = RequireMethod(typeof(NHealthBar), "RefreshText", BindingFlags.Instance | BindingFlags.NonPublic);

		harmony.Patch(
			loseHpInternal,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastLoseHpInternalPrefix)));
		harmony.Patch(
			currentHpSetter,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastCurrentHpSetterPrefix)));
		harmony.Patch(
			isAliveGetter,
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastIsAlivePostfix)));
		harmony.Patch(
			isDeadGetter,
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastIsDeadPostfix)));
		harmony.Patch(
			gainBlock,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastGainBlockPrefix)));
		harmony.Patch(
			gainBlockVar,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastGainBlockVarPrefix)));
		harmony.Patch(
			kill,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillPrefix)));
		harmony.Patch(
			killMany,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillManyPrefix)));
		harmony.Patch(
			killWithoutCheckingWinCondition,
			prefix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastKillPrefix)));
		harmony.Patch(
			healthBarRefreshText,
			postfix: new HarmonyMethod(typeof(HextechCombatHooks), nameof(NearDeathFeastHealthBarRefreshTextPostfix)));
	}

	private static void EnsureNearDeathFeastFields()
	{
		HealthBarCreatureField ??= RequireField(typeof(NHealthBar), "_creature");
		HealthBarHpLabelField ??= RequireField(typeof(NHealthBar), "_hpLabel");
	}

	private static bool NearDeathFeastLoseHpInternalPrefix(Creature __instance, decimal amount, ValueProp props, ref DamageResult __result)
	{
		if (NearDeathFeastRune.ShouldInterceptLoseHp(__instance, amount))
		{
			__result = NearDeathFeastRune.LoseHpAllowingDying(__instance, amount, props);
			return false;
		}

		if (HextechEnemyNearDeath.ShouldInterceptLoseHp(__instance, amount))
		{
			__result = HextechEnemyNearDeath.LoseHpAllowingDying(__instance, amount, props);
			return false;
		}

		return true;
	}

	private static bool NearDeathFeastCurrentHpSetterPrefix(Creature __instance, int value)
	{
		if (value >= 0)
		{
			// 敌方转阶段/接续/复活把 HP 设回正值:清掉残留的濒死状态。
			// 注意只认 >1:濒死维持本身就是把 HP 写成 1(LoseHpAllowingDying 内部的
			// SetCurrentHpInternal 会走本 setter),按 1 清会把刚记下的负血债务当场抹掉。
			if (value > 1)
			{
				HextechEnemyNearDeath.ClearIfRecovered(__instance, value);
			}

			return true;
		}

		if (NearDeathFeastRune.HasDyingState(__instance))
		{
			NearDeathFeastRune.PreserveNegativeHpAsDyingState(__instance, value);
			return false;
		}

		if (HextechEnemyNearDeath.HasDyingState(__instance))
		{
			HextechEnemyNearDeath.PreserveNegativeHpAsDyingState(__instance, value);
			return false;
		}

		return true;
	}

	private static void NearDeathFeastIsAlivePostfix(Creature __instance, ref bool __result)
	{
		if (!__result && (NearDeathFeastRune.IsDyingButAlive(__instance) || HextechEnemyNearDeath.IsDyingButAlive(__instance)))
		{
			__result = true;
		}
	}

	private static void NearDeathFeastIsDeadPostfix(Creature __instance, ref bool __result)
	{
		if (__result && (NearDeathFeastRune.IsDyingButAlive(__instance) || HextechEnemyNearDeath.IsDyingButAlive(__instance)))
		{
			__result = false;
		}
	}

	private static bool NearDeathFeastGainBlockPrefix(Creature creature, ref Task<decimal> __result)
	{
		if (!NearDeathFeastRune.ShouldPreventSustain(creature) && !HextechEnemyNearDeath.ShouldPreventSustain(creature))
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
		HextechEnemyNearDeath.ForceDeathThresholdForKill(creature);
	}

	private static void NearDeathFeastKillManyPrefix(IReadOnlyCollection<Creature> creatures)
	{
		foreach (Creature creature in creatures)
		{
			NearDeathFeastRune.ForceDeathThresholdForKill(creature);
			HextechEnemyNearDeath.ForceDeathThresholdForKill(creature);
		}
	}

	private static void NearDeathFeastHealthBarRefreshTextPostfix(NHealthBar __instance)
	{
		if (HealthBarCreatureField?.GetValue(__instance) is not Creature creature
			|| HealthBarHpLabelField?.GetValue(__instance) is not MegaLabel hpLabel)
		{
			return;
		}

		if (NearDeathFeastRune.TryGetDisplayedHp(creature, out int displayedHp)
			|| HextechEnemyNearDeath.TryGetDisplayedHp(creature, out displayedHp))
		{
			hpLabel.SetTextAutoSize($"{displayedHp}/{creature.MaxHp}");
		}
	}
}

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static readonly HextechScopedDepthGuard OutbreakPowerPoisonResponseGuard = new();
	private static readonly HextechScopedDepthGuard SleightOfFleshPowerDebuffResponseGuard = new();
	private static readonly HextechScopedDepthGuard CompensationReplacementGuard = new();

	internal static bool IsResolvingOutbreakPowerPoisonResponse => OutbreakPowerPoisonResponseGuard.IsActive;
	internal static bool IsResolvingSleightOfFleshPowerDebuffResponse => SleightOfFleshPowerDebuffResponseGuard.IsActive;
	internal static bool IsApplyingCompensationReplacement => CompensationReplacementGuard.IsActive;

	internal static void QueueInstantDeathDoomKill(Creature creature)
	{
		if (!PendingInstantDeathDoomKills.Contains(creature))
		{
			PendingInstantDeathDoomKills.Add(creature);
		}
	}

	private static async Task FlushPendingInstantDeathDoomKillsIfSafe()
	{
		if (SleightOfFleshPowerDebuffResponseGuard.IsActive || OutbreakPowerPoisonResponseGuard.IsActive)
		{
			return;
		}

		while (PendingInstantDeathDoomKills.Count > 0)
		{
			Creature creature = PendingInstantDeathDoomKills[0];
			PendingInstantDeathDoomKills.RemoveAt(0);
			if (creature.IsAlive && creature.GetPowerAmount<DoomPower>() > creature.CurrentHp)
			{
				await DoomPower.DoomKill([creature]);
			}
		}
	}

	// 疫情补丁本体随版本不同（0.110+ 技能牌 / 之前的 OutbreakPower），见 .Official.cs / .Legacy.cs。
	private static void WrapOutbreakResponse(ref bool entered, ref Task result)
	{
		if (!entered)
		{
			return;
		}

		result = OutbreakPowerPoisonResponseGuard.WrapEnteredTask(result, FlushPendingInstantDeathDoomKillsIfSafe);
		entered = false;
	}

	private static bool IsSleightOfFleshPowerDebuffResponse(SleightOfFleshPower instance, PowerModel power, decimal amount, Creature? applier)
	{
		return amount != 0m
			&& power.GetTypeForAmount(amount) == PowerType.Debuff
			&& power.Owner.IsEnemy
			&& applier == instance.Owner
			&& power is not ITemporaryPower;
	}

	internal static bool ShouldSuppressSleightOfFleshPowerDebuffResponse(bool wouldRespond)
	{
		return wouldRespond && IsApplyingCompensationReplacement;
	}

	internal static Task RunWithOutbreakPowerPoisonResponseGuard(Func<Task> action)
	{
		return OutbreakPowerPoisonResponseGuard.RunAsync(action);
	}

	internal static Task RunWithCompensationReplacementGuard(Func<Task> action)
	{
		return CompensationReplacementGuard.RunAsync(action);
	}

	internal static Task RunWithSleightOfFleshPowerDebuffResponseGuard(Func<Task> action)
	{
		return SleightOfFleshPowerDebuffResponseGuard.RunAsync(action);
	}

	[HarmonyPatch(typeof(SleightOfFleshPower), nameof(SleightOfFleshPower.AfterPowerAmountChanged), typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal), typeof(Creature), typeof(CardModel))]
	[HextechPatch("combat.sleight-of-flesh", "即死与补偿安全边界")]
	private static class SleightOfFleshPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(SleightOfFleshPower __instance, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource, ref Task __result, out bool __state)
		{
			__state = false;
			bool wouldRespond = IsSleightOfFleshPowerDebuffResponse(__instance, power, amount, applier);
			if (ShouldSuppressSleightOfFleshPowerDebuffResponse(wouldRespond))
			{
				__result = Task.CompletedTask;
				return false;
			}

			if (wouldRespond)
			{
				__state = true;
				SleightOfFleshPowerDebuffResponseGuard.Enter();
			}

			return true;
		}

		[HarmonyPostfix]
		private static void Postfix(ref bool __state, ref Task __result)
		{
			if (!__state)
			{
				return;
			}

			__result = SleightOfFleshPowerDebuffResponseGuard.WrapEnteredTask(__result, FlushPendingInstantDeathDoomKillsIfSafe);
			__state = false;
		}

		[HarmonyFinalizer]
		private static Exception? Finalizer(bool __state, Exception? __exception) => ExitGuardAfterSynchronousFailure(SleightOfFleshPowerDebuffResponseGuard, __state, __exception);
	}
}

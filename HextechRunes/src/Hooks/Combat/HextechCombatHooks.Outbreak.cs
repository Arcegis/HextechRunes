using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static readonly AsyncLocal<int> OutbreakPowerPoisonResponseDepth = new();
	private static readonly AsyncLocal<int> SleightOfFleshPowerDebuffResponseDepth = new();
	private static readonly AsyncLocal<int> CompensationReplacementDoomDepth = new();

	internal static bool IsResolvingOutbreakPowerPoisonResponse => OutbreakPowerPoisonResponseDepth.Value > 0;
	internal static bool IsResolvingSleightOfFleshPowerDebuffResponse => SleightOfFleshPowerDebuffResponseDepth.Value > 0;
	internal static bool IsApplyingCompensationReplacementDoom => CompensationReplacementDoomDepth.Value > 0;

	private static void OutbreakPowerAfterPowerAmountChangedPrefix(OutbreakPower __instance, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource, out bool __state)
	{
		__state = amount > 0m
			&& applier == __instance.Owner
			&& power is PoisonPower;
		if (__state)
		{
			OutbreakPowerPoisonResponseDepth.Value++;
		}
	}

	private static void OutbreakPowerAfterPowerAmountChangedPostfix(bool __state, ref Task __result)
	{
		if (__state)
		{
			__result = CompleteWithOutbreakPowerPoisonResponseReset(__result);
		}
	}

	private static bool SleightOfFleshPowerAfterPowerAmountChangedPrefix(SleightOfFleshPower __instance, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource, ref Task __result, out bool __state)
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
			SleightOfFleshPowerDebuffResponseDepth.Value++;
		}

		return true;
	}

	private static void SleightOfFleshPowerAfterPowerAmountChangedPostfix(bool __state, ref Task __result)
	{
		if (__state)
		{
			__result = CompleteWithSleightOfFleshPowerDebuffResponseReset(__result);
		}
	}

	private static async Task CompleteWithOutbreakPowerPoisonResponseReset(Task task)
	{
		try
		{
			await task;
		}
		finally
		{
			OutbreakPowerPoisonResponseDepth.Value = Math.Max(0, OutbreakPowerPoisonResponseDepth.Value - 1);
		}
	}

	private static async Task CompleteWithSleightOfFleshPowerDebuffResponseReset(Task task)
	{
		try
		{
			await task;
		}
		finally
		{
			SleightOfFleshPowerDebuffResponseDepth.Value = Math.Max(0, SleightOfFleshPowerDebuffResponseDepth.Value - 1);
		}
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
		return wouldRespond && IsApplyingCompensationReplacementDoom;
	}

	internal static async Task RunWithCompensationReplacementDoomGuard(Func<Task> action)
	{
		CompensationReplacementDoomDepth.Value++;
		try
		{
			await action();
		}
		finally
		{
			CompensationReplacementDoomDepth.Value = Math.Max(0, CompensationReplacementDoomDepth.Value - 1);
		}
	}
}

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static readonly AsyncLocal<int> OutbreakPowerPoisonResponseDepth = new();
	private static readonly AsyncLocal<int> SleightOfFleshPowerDebuffResponseDepth = new();
	private static readonly AsyncLocal<int> CompensationReplacementDepth = new();

	// 即死符文在血肉戏法/疫情响应链内不能同步 DoomKill(死亡处理与进行中的
	// power hook 链撞车会卡死游戏),先挂账,响应链退出后统一补杀。
	private static readonly List<Creature> PendingInstantDeathDoomKills = [];

	internal static bool IsResolvingOutbreakPowerPoisonResponse => OutbreakPowerPoisonResponseDepth.Value > 0;
	internal static bool IsResolvingSleightOfFleshPowerDebuffResponse => SleightOfFleshPowerDebuffResponseDepth.Value > 0;
	internal static bool IsApplyingCompensationReplacement => CompensationReplacementDepth.Value > 0;

	internal static void QueueInstantDeathDoomKill(Creature creature)
	{
		if (!PendingInstantDeathDoomKills.Contains(creature))
		{
			PendingInstantDeathDoomKills.Add(creature);
		}
	}

	private static async Task FlushPendingInstantDeathDoomKillsIfSafe()
	{
		if (SleightOfFleshPowerDebuffResponseDepth.Value > 0 || OutbreakPowerPoisonResponseDepth.Value > 0)
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

		await FlushPendingInstantDeathDoomKillsIfSafe();
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

		await FlushPendingInstantDeathDoomKillsIfSafe();
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

	internal static async Task RunWithOutbreakPowerPoisonResponseGuard(Func<Task> action)
	{
		OutbreakPowerPoisonResponseDepth.Value++;
		try
		{
			await action();
		}
		finally
		{
			OutbreakPowerPoisonResponseDepth.Value = Math.Max(0, OutbreakPowerPoisonResponseDepth.Value - 1);
		}
	}

	internal static async Task RunWithCompensationReplacementGuard(Func<Task> action)
	{
		CompensationReplacementDepth.Value++;
		try
		{
			await action();
		}
		finally
		{
			CompensationReplacementDepth.Value = Math.Max(0, CompensationReplacementDepth.Value - 1);
		}
	}

	internal static async Task RunWithSleightOfFleshPowerDebuffResponseGuard(Func<Task> action)
	{
		SleightOfFleshPowerDebuffResponseDepth.Value++;
		try
		{
			await action();
		}
		finally
		{
			SleightOfFleshPowerDebuffResponseDepth.Value = Math.Max(0, SleightOfFleshPowerDebuffResponseDepth.Value - 1);
		}
	}
}

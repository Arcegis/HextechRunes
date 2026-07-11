namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static bool GainMaxHpPrefix(Creature creature, ref decimal amount, ref Task __result, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp
			|| creature.Player is not Player player
			|| HextechMaxHpScaling.GetPrimary(player) is not IHextechMaxHpBaseHolder primary)
		{
			return true;
		}

		HextechMaxHpScaling.EnsureBaseInitialized(player, primary, assumeAlreadyScaled: true);
		int oldActual = creature.MaxHp;
		primary.BaseMaxHp += (int)amount;
		int newActual = HextechMaxHpScaling.GetScaledMaxHp(player, primary);
		int delta = Math.Max(0, newActual - oldActual);
		if (delta == 0)
		{
			__result = Task.CompletedTask;
			return false;
		}

		_handlingGoliathMaxHp = true;
		__state = true;
		amount = delta;
		return true;
	}

	private static bool LoseMaxHpPrefix(Creature creature, ref decimal amount, ref Task __result, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp
			|| creature.Player is not Player player
			|| HextechMaxHpScaling.GetPrimary(player) is not IHextechMaxHpBaseHolder primary)
		{
			return true;
		}

		HextechMaxHpScaling.EnsureBaseInitialized(player, primary, assumeAlreadyScaled: true);
		int oldActual = creature.MaxHp;
		primary.BaseMaxHp -= (int)amount;
		int newActual = HextechMaxHpScaling.GetScaledMaxHp(player, primary);
		int loss = Math.Max(0, oldActual - newActual);
		if (loss == 0)
		{
			__result = Task.CompletedTask;
			return false;
		}

		_handlingGoliathMaxHp = true;
		__state = true;
		amount = loss;
		return true;
	}

	private static bool SetMaxHpPrefix(Creature creature, ref decimal amount, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp
			|| creature.Player is not Player player
			|| HextechMaxHpScaling.GetPrimary(player) is not IHextechMaxHpBaseHolder primary)
		{
			return true;
		}

		primary.BaseMaxHp = (int)Math.Max(1m, amount);
		_handlingGoliathMaxHp = true;
		__state = true;
		amount = HextechMaxHpScaling.GetScaledMaxHp(player, primary);
		return true;
	}

	private static void ResetGoliathTaskPostfix(Creature creature, bool __state, ref Task __result)
	{
		if (__state || creature.Player?.GetRelic<NearDeathFeastRune>() != null)
		{
			__result = CompleteWithMaxHpPostfix(__result, __state, creature);
		}
	}

	private static void ResetGoliathDecimalTaskPostfix(Creature creature, bool __state, ref Task<decimal> __result)
	{
		if (__state || creature.Player?.GetRelic<NearDeathFeastRune>() != null)
		{
			__result = CompleteWithMaxHpPostfix(__result, __state, creature);
		}
	}

	private static async Task CompleteWithMaxHpPostfix(Task task, bool resetGoliath, Creature creature)
	{
		try
		{
			await task;
		}
		finally
		{
			if (resetGoliath)
			{
				_handlingGoliathMaxHp = false;
			}
			creature.Player?.GetRelic<NearDeathFeastRune>()?.RefreshDeathLimitDisplay();
		}
	}

	private static async Task<decimal> CompleteWithMaxHpPostfix(Task<decimal> task, bool resetGoliath, Creature creature)
	{
		try
		{
			return await task;
		}
		finally
		{
			if (resetGoliath)
			{
				_handlingGoliathMaxHp = false;
			}
			creature.Player?.GetRelic<NearDeathFeastRune>()?.RefreshDeathLimitDisplay();
		}
	}
}

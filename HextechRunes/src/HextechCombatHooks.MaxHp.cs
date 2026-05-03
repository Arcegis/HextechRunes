using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static bool GainMaxHpPrefix(Creature creature, ref decimal amount, ref Task __result, out bool __state)
	{
		__state = false;
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.EnsureBaseMaxHpInitialized();
		int oldActual = creature.MaxHp;
		rune.BaseMaxHp += (int)amount;
		int newActual = rune.GetScaledMaxHp();
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
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.EnsureBaseMaxHpInitialized();
		int oldActual = creature.MaxHp;
		rune.BaseMaxHp -= (int)amount;
		int newActual = rune.GetScaledMaxHp();
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
		if (_handlingGoliathMaxHp || creature.Player?.GetRelic<GoliathRune>() is not GoliathRune rune)
		{
			return true;
		}

		rune.BaseMaxHp = (int)Math.Max(1m, amount);
		_handlingGoliathMaxHp = true;
		__state = true;
		amount = rune.GetScaledMaxHp();
		return true;
	}

	private static void ResetGoliathTaskPostfix(bool __state, ref Task __result)
	{
		if (__state)
		{
			__result = CompleteWithReset(__result);
		}
	}

	private static void ResetGoliathDecimalTaskPostfix(bool __state, ref Task<decimal> __result)
	{
		if (__state)
		{
			__result = CompleteWithReset(__result);
		}
	}

	private static async Task CompleteWithReset(Task task)
	{
		try
		{
			await task;
		}
		finally
		{
			_handlingGoliathMaxHp = false;
		}
	}

	private static async Task<decimal> CompleteWithReset(Task<decimal> task)
	{
		try
		{
			return await task;
		}
		finally
		{
			_handlingGoliathMaxHp = false;
		}
	}
}

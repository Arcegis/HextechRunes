using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static int _actualDamageCommandDepth;

	internal static bool IsResolvingActualDamageCommand => _actualDamageCommandDepth > 0;

	private static void ActualDamageCommandPrefix(out bool __state)
	{
		_actualDamageCommandDepth++;
		__state = true;
	}

	private static void ActualDamageCommandPostfix(bool __state, ref Task<IEnumerable<DamageResult>> __result)
	{
		if (__state)
		{
			__result = CompleteWithActualDamageCommandReset(__result);
		}
	}

	private static void ActualAttackCommandPostfix(bool __state, ref Task<AttackCommand> __result)
	{
		if (__state)
		{
			__result = CompleteWithActualDamageCommandReset(__result);
		}
	}

	private static async Task<T> CompleteWithActualDamageCommandReset<T>(Task<T> task)
	{
		try
		{
			T result = await task;
			// Zeroed damage can skip AfterDamageGiven; apply any leftover Pacifist conversions before multiplayer checksums.
			await PacifistRune.FlushPendingDoomApplications();
			return result;
		}
		finally
		{
			_actualDamageCommandDepth = Math.Max(0, _actualDamageCommandDepth - 1);
			if (_actualDamageCommandDepth == 0)
			{
				PacifistRune.ClearPendingDoomApplications();
			}
		}
	}
}

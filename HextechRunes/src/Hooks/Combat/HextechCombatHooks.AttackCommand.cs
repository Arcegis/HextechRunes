using MegaCrit.Sts2.Core.Commands.Builders;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static void AttackCommandExecutePostfix(AttackCommand __instance, ref Task<AttackCommand> __result)
	{
		__result = EnsureAttackCommandExecuteResult(__result, __instance);
	}

	internal static async Task<AttackCommand> EnsureAttackCommandExecuteResult(Task<AttackCommand>? task, AttackCommand command)
	{
		if (task == null)
		{
			return command;
		}

		return await task ?? command;
	}
}

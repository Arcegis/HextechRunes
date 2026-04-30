using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HextechRunes;

internal static class CreatureCmdCompat
{
	private static readonly MethodInfo SetMaxHpMethod = typeof(CreatureCmd).GetMethod(
		nameof(CreatureCmd.SetMaxHp),
		BindingFlags.Public | BindingFlags.Static,
		binder: null,
		[typeof(Creature), typeof(decimal)],
		modifiers: null)
		?? throw new InvalidOperationException("Could not find CreatureCmd.SetMaxHp(Creature, decimal).");

	internal static async Task SetMaxHp(Creature creature, decimal amount)
	{
		try
		{
			object? result = SetMaxHpMethod.Invoke(null, [creature, amount]);
			switch (result)
			{
				case Task<decimal> decimalTask:
					await decimalTask;
					return;
				case Task task:
					await task;
					return;
				default:
					throw new InvalidOperationException($"Unexpected CreatureCmd.SetMaxHp return type: {result?.GetType().FullName ?? "null"}.");
			}
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			throw ex.InnerException;
		}
	}
}

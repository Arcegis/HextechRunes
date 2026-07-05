using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class CreatureCmdCompat
{
	private static readonly MethodInfo SetMaxHpMethod = RequireMethod(
		typeof(CreatureCmd),
		nameof(CreatureCmd.SetMaxHp),
		BindingFlags.Public | BindingFlags.Static,
		typeof(Creature),
		typeof(decimal));

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

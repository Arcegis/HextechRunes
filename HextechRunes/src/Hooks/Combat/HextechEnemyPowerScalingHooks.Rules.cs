namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	private static ScalingOverride? GetScalingOverride(Type powerType)
	{
		if (powerType == typeof(ArtifactPower) || powerType == typeof(SlipperyPower))
		{
			return ScalingOverride.PlayerCount;
		}

		if (powerType == typeof(HardenedShellPower)
			|| powerType == typeof(RegenPower)
			|| powerType == typeof(PlatingPower)
			|| powerType == typeof(ReflectPower)
			|| powerType == typeof(SkittishPower))
		{
			return ScalingOverride.Unscaled;
		}

		return null;
	}

	private static int GetPlayerCount(Creature? giver, Creature target)
	{
		return target.CombatState?.Players.Count
			?? giver?.CombatState?.Players.Count
			?? 1;
	}

	private static decimal MultiplyByPlayerCount(decimal amount, int playerCount)
	{
		int scale = Math.Clamp(playerCount, 1, 16);
		if (scale <= 1)
		{
			return ClampPowerAmount(amount);
		}

		if (amount >= int.MaxValue / scale)
		{
			return int.MaxValue;
		}
		if (amount <= int.MinValue / scale)
		{
			return int.MinValue;
		}

		try
		{
			return ClampPowerAmount(amount * scale);
		}
		catch (OverflowException)
		{
			return amount < 0m ? int.MinValue : int.MaxValue;
		}
	}

	private static decimal ClampPowerAmount(decimal amount)
	{
		if (amount > int.MaxValue)
		{
			return int.MaxValue;
		}
		if (amount < int.MinValue)
		{
			return int.MinValue;
		}

		return amount;
	}

	private static OverrideScope BeginOverride(ScalingOverride scalingOverride)
	{
		return new OverrideScope(scalingOverride);
	}

	private sealed class OverrideScope : IDisposable
	{
		private readonly ScalingOverride? _previousOverride;

		public OverrideScope(ScalingOverride scalingOverride)
		{
			_previousOverride = CurrentOverride.Value;
			CurrentOverride.Value = scalingOverride;
		}

		public void Dispose()
		{
			CurrentOverride.Value = _previousOverride;
		}
	}
}

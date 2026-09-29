namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	// 本模组施加给敌人时改写原版联机缩放口径的能力：PlayerCount 按玩家数放大（原版对它们不缩放或缩放口径不同），
	// Unscaled 不随玩家数放大。补丁目标（各类型的 GetScaledAmountForMultiplayer 声明处）也由这张表派生。
	private static readonly Dictionary<Type, ScalingOverride> ScalingOverrides = new()
	{
		[typeof(ArtifactPower)] = ScalingOverride.PlayerCount,
		[typeof(SlipperyPower)] = ScalingOverride.PlayerCount,
		[typeof(HardenedShellPower)] = ScalingOverride.Unscaled,
		[typeof(RegenPower)] = ScalingOverride.Unscaled,
		[typeof(PlatingPower)] = ScalingOverride.Unscaled,
		[typeof(ReflectPower)] = ScalingOverride.Unscaled,
		[typeof(SkittishPower)] = ScalingOverride.Unscaled
	};

	private static ScalingOverride? GetScalingOverride(Type powerType)
	{
		return ScalingOverrides.TryGetValue(powerType, out ScalingOverride scalingOverride) ? scalingOverride : null;
	}

	private static int GetPlayerCount(Creature? giver, Creature target)
	{
		return target.CombatState?.Players.Count
			?? giver?.CombatState?.Players.Count
			?? 1;
	}

	private static decimal MultiplyByPlayerCount(decimal amount, int playerCount)
	{
		int scale = HextechEnemyHexContext.ClampScalingPlayerCount(playerCount);
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

		// 上面两道边界已保证乘积落在 int 范围内，decimal 乘法不会溢出。
		return ClampPowerAmount(amount * scale);
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

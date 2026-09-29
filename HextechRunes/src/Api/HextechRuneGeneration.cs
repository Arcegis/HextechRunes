using System.Diagnostics.CodeAnalysis;

namespace HextechRunes;

/// <summary>候选生成完成后的扩展点。不得消耗共享 RNG 或修改已有候选实例。</summary>
public static class HextechRuneGeneration
{
	public delegate List<RelicModel> CandidateTransform(Player player, HextechRarityTier rarity,
		RunState runState, int stage, int chancePercent, IReadOnlyList<RelicModel> options, int slot, int rerollOrdinal);

	private static readonly object SyncRoot = new();
	private static CandidateTransform? _chaosTransform;

	public static bool ChaosAvailable
	{
		get
		{
			lock (SyncRoot)
			{
				return _chaosTransform != null;
			}
		}
	}

	public static void RegisterChaosTransform(CandidateTransform transform)
	{
		ArgumentNullException.ThrowIfNull(transform);
		lock (SyncRoot)
		{
			if (_chaosTransform != null && _chaosTransform != transform)
			{
				throw new InvalidOperationException("A chaos rune generator is already registered.");
			}

			_chaosTransform = transform;
		}
	}

	/// <summary>注销先前登记的同一个委托;不是当前登记的委托时不做任何事并返回 false。</summary>
	public static bool UnregisterChaosTransform(CandidateTransform transform)
	{
		ArgumentNullException.ThrowIfNull(transform);
		lock (SyncRoot)
		{
			if (_chaosTransform != transform)
			{
				return false;
			}

			_chaosTransform = null;
			return true;
		}
	}

	/// <summary>
	/// 对已生成的候选调用第三方变换。结果直接进入联机选择协议,所以第三方委托抛异常、返回 null、
	/// 改变候选条数或返回非海克斯玩家符文时,一律告警并退回原候选,不让外部错误打断或污染选择流程。
	/// </summary>
	internal static List<RelicModel> Transform(Player player, HextechRarityTier rarity,
		RunState runState, int stage, List<RelicModel> options, int slot = -1, int rerollOrdinal = 0)
	{
		CandidateTransform? transform;
		lock (SyncRoot)
		{
			transform = _chaosTransform;
		}

		if (transform == null || options.Count == 0)
		{
			return options;
		}

		if (HextechMayhemModifier.FindIn(runState) is not { IsModActiveForRun: true } modifier)
		{
			return options;
		}

		int chancePercent = modifier.GetEffectiveRunConfigurationSnapshot().ChaosRuneChancePercent;
		if (chancePercent <= 0)
		{
			return options;
		}

		List<RelicModel>? transformed;
		try
		{
			transformed = transform(player, rarity, runState, stage, chancePercent, options, slot, rerollOrdinal);
		}
		catch (Exception ex)
		{
			WarnRejected($"threw {ex.GetType().Name}: {ex.Message}");
			return options;
		}

		if (!TryAcceptTransformResult(options, transformed, out string? rejection))
		{
			WarnRejected(rejection);
			return options;
		}

		return transformed;
	}

	internal static bool TryAcceptTransformResult(
		IReadOnlyList<RelicModel> options,
		[NotNullWhen(true)] List<RelicModel>? transformed,
		[NotNullWhen(false)] out string? rejection)
	{
		if (transformed == null)
		{
			rejection = "returned null";
			return false;
		}

		if (transformed.Count != options.Count)
		{
			rejection = $"changed the option count from {options.Count} to {transformed.Count}";
			return false;
		}

		foreach (RelicModel? relic in transformed)
		{
			if (!HextechCatalog.IsHextechRelic(relic))
			{
				rejection = $"returned a non-hextech player rune: {relic?.GetType().FullName ?? "null"}";
				return false;
			}
		}

		rejection = null;
		return true;
	}

	private static void WarnRejected(string reason)
	{
		if (HextechRunLogBudget.TryConsume("rune-generation.transform-rejected", 12))
		{
			HextechLog.Warn("RuneGeneration", $"Chaos rune transform {reason}; keeping the original candidates.");
		}
	}
}

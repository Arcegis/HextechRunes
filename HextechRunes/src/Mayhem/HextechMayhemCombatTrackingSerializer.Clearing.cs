namespace HextechRunes;

internal static partial class HextechMayhemCombatTrackingSerializer
{
	// 启动期 fail-fast:任何 *ThisTurn / *ThisPlayerTurn 字段都必须声明 [CombatTrackingClear],
	// 否则抛异常。把"新增逐回合字段却忘了清空"从静默 bug 变成启动崩溃,
	// 与 ValidateStateFieldCoverage 对序列化的强制对齐。先于清单字段初始化。
	private static readonly bool ClearPhaseCoverageValidated = ValidateClearPhaseCoverage();
	private static readonly IReadOnlyList<FieldInfo> PlayerTurnStartClearFields = CreateClearFields(CombatTrackingClearPhase.PlayerTurnStart);
	private static readonly IReadOnlyList<FieldInfo> PlayerTurnEndClearFields = CreateClearFields(CombatTrackingClearPhase.PlayerTurnEnd);
	private static readonly IReadOnlyList<FieldInfo> EnemyTurnStartClearFields = CreateClearFields(CombatTrackingClearPhase.EnemyTurnStart);

	public static void ClearPhase(HextechMayhemCombatTrackingState state, CombatTrackingClearPhase phase)
	{
		foreach (FieldInfo field in GetClearFields(phase))
		{
			ClearStateField(field, state);
		}
	}

	private static IReadOnlyList<FieldInfo> GetClearFields(CombatTrackingClearPhase phase)
	{
		return phase switch
		{
			CombatTrackingClearPhase.PlayerTurnStart => PlayerTurnStartClearFields,
			CombatTrackingClearPhase.PlayerTurnEnd => PlayerTurnEndClearFields,
			CombatTrackingClearPhase.EnemyTurnStart => EnemyTurnStartClearFields,
			_ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "ClearPhase expects exactly one clear phase."),
		};
	}

	private static IReadOnlyList<FieldInfo> CreateClearFields(CombatTrackingClearPhase phase)
	{
		return GetPublicStateFields()
			.Where(field => field.GetCustomAttribute<CombatTrackingClearAttribute>() is { } attribute
				&& (attribute.Phases & phase) != 0)
			.ToArray();
	}

	private static bool ValidateClearPhaseCoverage()
	{
		string[] unmarked = GetPublicStateFields()
			.Where(static field => field.Name.Contains("ThisTurn", StringComparison.Ordinal)
				|| field.Name.Contains("ThisPlayerTurn", StringComparison.Ordinal))
			.Where(static field => field.GetCustomAttribute<CombatTrackingClearAttribute>() == null)
			.Select(static field => field.Name)
			.OrderBy(static name => name, StringComparer.Ordinal)
			.ToArray();
		if (unmarked.Length > 0)
		{
			throw new InvalidOperationException(
				$"Combat tracking per-turn fields must declare [CombatTrackingClear]: {string.Join(", ", unmarked)}.");
		}

		return true;
	}
}

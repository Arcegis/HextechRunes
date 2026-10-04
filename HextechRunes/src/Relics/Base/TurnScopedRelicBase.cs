namespace HextechRunes;

/// <summary>
/// 带"每回合"状态（每回合一次、每回合 N 次）的符文。战斗开始、战斗结束、持有者阵营回合开始时由本类统一清零；
/// 回合开始钩子已由 <see cref="HextechRelicBase"/> 按参与者过滤，队友的额外回合不会清零，持有者自己的额外回合会。
/// 回合中途用 <see cref="EnsureTurnScopedStateCurrent()"/> 按持有者回合号懒清零（读档、钩子之外的读取）。
/// </summary>
/// <remarks>
/// 子类只在 <see cref="ResetTurnScopedState"/> 里清字段、刷新显示，回合身份由本类维护。
/// 子类覆写这三个钩子做别的事时先调用 base。
/// </remarks>
public abstract class TurnScopedRelicBase : HextechRelicBase
{
	protected abstract void ResetTurnScopedState();

	public override Task BeforeCombatStart()
	{
		ResetTurnScopedStateAndIdentity(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnScopedStateAndIdentity(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (side == Owner.Creature.Side)
		{
			ResetTurnScopedStateAndIdentity(combatState);
		}

		return Task.CompletedTask;
	}

	protected void EnsureTurnScopedStateCurrent()
	{
		EnsureTurnScopedStateCurrent(ResetTurnScopedState);
	}

	private void ResetTurnScopedStateAndIdentity(HextechCombatState? combatState)
	{
		ResetTurnScopedState();
		UpdateTurnScopedStateIdentity(combatState);
	}
}

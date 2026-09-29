namespace HextechRunes;

/// <summary>
/// 战斗胜利后的共享结算。联机时由 Mayhem 在 AfterCombatVictory 里按玩家顺序统一调用（两端顺序一致），
/// 单机由符文自己的 AfterCombatVictory 调用。
/// </summary>
public interface IHextechSharedCombatVictoryRune
{
	Task ApplySharedCombatVictory(CombatRoom room);
}

/// <summary>
/// <see cref="IHextechSharedCombatVictoryRune"/> 的单机入口：联机跳过（交给 Mayhem 统一调用），单机直接结算。
/// 已有其他基类、不能继承本类的实现按同样写法自己覆写 AfterCombatVictory。
/// </summary>
public abstract class HextechSharedCombatVictoryRuneBase : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	public sealed override Task AfterCombatVictory(CombatRoom room)
	{
		return IsNetworkMultiplayer()
			? Task.CompletedTask
			: ApplySharedCombatVictory(room);
	}

	public abstract Task ApplySharedCombatVictory(CombatRoom room);
}

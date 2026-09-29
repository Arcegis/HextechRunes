namespace HextechRunes;

/// <summary>
/// 经 global alias <c>PowerCmd</c> 替代原版 <c>MegaCrit.Sts2.Core.Commands.PowerCmd</c>（本体与拓展包都用，所以保持 public）。
/// 名字沿用历史，其实没有版本差异：真实职责是给没有 choiceContext 的调用点补一个 <see cref="BlockingPlayerChoiceContext"/>。
/// 原版会把 choiceContext 传给 <c>Hook.AfterPowerAmountChanged</c>；Hook 自带的 context 在玩家做选择时会放行其他玩家的命令队列，
/// Blocking 则不放行，二者联机时序不同，所以手上有 context 的旧调用点暂不改传，避免改变结算行为。
/// </summary>
public static class HextechPowerCmdCompat
{
	public static Task<IReadOnlyList<T>> Apply<T>(
		IEnumerable<Creature> targets,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new BlockingPlayerChoiceContext(),
			targets,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task<T?> Apply<T>(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new BlockingPlayerChoiceContext(),
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task<T?> Apply<T>(
		PlayerChoiceContext? choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			choiceContext ?? new BlockingPlayerChoiceContext(),
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	// 旧签名只为已编译的拓展包二进制保留（源码调用会优先绑定上面的强类型重载）；非 PlayerChoiceContext 实参按原行为退回 Blocking。
	[Obsolete("Use the PlayerChoiceContext? overload.")]
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public static Task<T?> Apply<T>(
		object? choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return Apply<T>(choiceContext as PlayerChoiceContext, target, amount, applier, cardSource, silent);
	}

	public static Task Apply(
		PowerModel power,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply(
			new BlockingPlayerChoiceContext(),
			power,
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task Remove<T>(Creature creature)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove<T>(creature);
	}

	public static Task Remove(PowerModel power)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
	}

	public static Task Decrement(PowerModel power)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Decrement(power);
	}
}

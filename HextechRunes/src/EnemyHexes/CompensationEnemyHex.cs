using System.Diagnostics.CodeAnalysis;

namespace HextechRunes;

internal sealed class CompensationEnemyHex : HextechEnemyHexEffect
{
	// 效果是单例（HextechEnemyHexEffects），待结算项放静态列表：伤害命令结束时 HextechCombatHooks 按命令号清理。
	private static readonly List<PendingCompensation> PendingCompensations = [];

	internal override MonsterHexKind Kind => MonsterHexKind.Compensation;

	internal override void ResetRunScopedState()
	{
		PendingCompensations.Clear();
	}

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		PendingCompensations.Clear();
		return Task.CompletedTask;
	}

	internal override Task BeforeSideTurnStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		PendingCompensations.Clear();
		return Task.CompletedTask;
	}

	internal override Task AfterCombatVictory(HextechEnemyHexContext context, CombatRoom room)
	{
		PendingCompensations.Clear();
		return Task.CompletedTask;
	}

	internal override decimal ModifyHpLostAfterOsty(HextechEnemyHexContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		// 分发层已限定 target 在本局战斗中。
		if (target.Side != CombatSide.Enemy
			|| target.IsDead
			|| ShouldSkipDamageReplacement()
			|| amount <= 0m)
		{
			return amount;
		}

		long commandId = HextechCombatHooks.CurrentActualDamageCommandId;
		if (commandId == 0L)
		{
			return amount;
		}

		(decimal immediateDamage, int nextTurnDamage) = SplitDamage(amount);
		if (nextTurnDamage <= 0)
		{
			return amount;
		}

		EnqueuePendingCompensation(commandId, target, nextTurnDamage, dealer, cardSource);
		return immediateDamage;
	}

	internal override async Task AfterEnemyDamageReceivedAny(HextechEnemyHexContext context, Creature target, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		long commandId = HextechCombatHooks.CurrentActualDamageCommandId;
		if (commandId == 0L || !TryTakePendingCompensation(commandId, target, out PendingCompensation? compensation))
		{
			return;
		}

		if (!CanApplyPendingCompensation(context, target, compensation))
		{
			return;
		}

		Creature applier = compensation.Dealer is { IsAlive: true } ? compensation.Dealer : target;
		await HextechCombatHooks.RunWithCompensationReplacementGuard(
			() => PowerCmd.Apply<HextechNextTurnDamagePower>(target, compensation.Amount, applier, compensation.CardSource));
	}

	internal static void ClearPendingCompensations(long commandId)
	{
		PendingCompensations.RemoveAll(pending => pending.CommandId == commandId);
	}

	internal static (decimal ImmediateDamage, int NextTurnDamage) SplitDamage(decimal damage)
	{
		if (damage <= 0m)
		{
			return (damage, 0);
		}

		int nextTurnDamage = (int)Math.Min(Math.Floor(damage / 2m), HextechCreatureStatLimits.StatHardCap);
		return (damage - nextTurnDamage, nextTurnDamage);
	}

	internal static bool ShouldSkipDamageReplacement()
	{
		// 血肉戏法(Sleight of Flesh)在玩家给敌人施加 debuff 时会对该敌人造成一次伤害。
		// 这次伤害不能再被代偿延期,否则「血肉戏法伤害 → 下回合伤害(debuff) → 血肉戏法响应 → …」
		// 会无限递归直至栈溢出。源头切断这条边:代偿在血肉戏法响应期间不替换伤害。
		// 与「代偿施加下回合伤害时抑制血肉戏法响应」(RunWithCompensationReplacementGuard)构成双向防护。
		return HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse
			|| HextechCombatHooks.IsResolvingSleightOfFleshPowerDebuffResponse
			|| HextechNextTurnDamagePower.IsResolvingDamage;
	}

	private static void EnqueuePendingCompensation(long commandId, Creature target, decimal amount, Creature? dealer, CardModel? cardSource)
	{
		for (int i = PendingCompensations.Count - 1; i >= 0; i--)
		{
			PendingCompensation pending = PendingCompensations[i];
			if (pending.CommandId == commandId && pending.Target == target)
			{
				PendingCompensations[i] = pending with
				{
					Amount = pending.Amount + amount,
					Dealer = dealer ?? pending.Dealer,
					CardSource = cardSource ?? pending.CardSource
				};
				return;
			}
		}

		PendingCompensations.Add(new PendingCompensation(commandId, target, amount, dealer, cardSource));
	}

	private static bool TryTakePendingCompensation(long commandId, Creature target, [NotNullWhen(true)] out PendingCompensation? pending)
	{
		for (int i = 0; i < PendingCompensations.Count; i++)
		{
			pending = PendingCompensations[i];
			if (pending.CommandId != commandId || pending.Target != target)
			{
				continue;
			}

			PendingCompensations.RemoveAt(i);
			return true;
		}

		pending = null;
		return false;
	}

	private static bool CanApplyPendingCompensation(HextechEnemyHexContext context, Creature target, PendingCompensation compensation)
	{
		return compensation.Amount > 0m
			&& target.IsAlive
			&& target.CombatState?.RunState == context.RunState;
	}

	private sealed record PendingCompensation(long CommandId, Creature Target, decimal Amount, Creature? Dealer, CardModel? CardSource);
}

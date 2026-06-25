namespace HextechRunes;

internal sealed class CompensationEnemyHex : HextechEnemyHexEffect
{
	private static readonly HashSet<CompensationEnemyHex> EffectsWithPendingCompensation = new();

	private readonly List<PendingCompensation> _pendingCompensations = [];

	internal override MonsterHexKind Kind => MonsterHexKind.Compensation;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		ClearPendingCompensationsForEffect();
		return Task.CompletedTask;
	}

	internal override Task BeforeSideTurnStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		ClearPendingCompensationsForEffect();
		return Task.CompletedTask;
	}

	internal override Task AfterCombatVictory(HextechEnemyHexContext context, CombatRoom room)
	{
		ClearPendingCompensationsForEffect();
		return Task.CompletedTask;
	}

	internal override decimal ModifyHpLostAfterOsty(HextechEnemyHexContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target.Side != CombatSide.Enemy
			|| target.CombatState?.RunState != context.RunState
			|| target.IsDead
			|| ShouldSkipDamageReplacement(target, props, dealer, cardSource)
			|| amount <= 0m)
		{
			return amount;
		}

		long commandId = HextechCombatHooks.CurrentActualDamageCommandId;
		if (commandId == 0L)
		{
			return amount;
		}

		int poison = CalculateReplacementPoison(amount);
		if (poison <= 0)
		{
			return amount;
		}

		bool shouldConsumeSlippery = target.GetPowerAmount<SlipperyPower>() > 0m;
		EnqueuePendingCompensation(commandId, target, poison, dealer, cardSource, shouldConsumeSlippery);
		return 0m;
	}

	internal override async Task AfterEnemyDamageReceivedAny(HextechEnemyHexContext context, Creature target, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		long commandId = HextechCombatHooks.CurrentActualDamageCommandId;
		if (commandId == 0L || !TryTakePendingCompensation(commandId, target, out PendingCompensation? pending))
		{
			return;
		}

		PendingCompensation compensation = pending!;
		if (!CanApplyPendingCompensation(context, target, compensation))
		{
			return;
		}

		if (compensation.ShouldConsumeSlippery && target.GetPower<SlipperyPower>() is SlipperyPower slippery)
		{
			await PowerCmd.Decrement(slippery);
		}

		Creature applier = compensation.Dealer is { IsAlive: true } ? compensation.Dealer : target;
		await HextechCombatHooks.RunWithCompensationReplacementGuard(
			() => PowerCmd.Apply<PoisonPower>(target, compensation.Amount, applier, compensation.CardSource));
	}

	internal static void ClearPendingCompensations(long commandId)
	{
		if (EffectsWithPendingCompensation.Count == 0)
		{
			return;
		}

		CompensationEnemyHex[] effects = EffectsWithPendingCompensation.ToArray();
		foreach (CompensationEnemyHex effect in effects)
		{
			effect.ClearPendingCompensationsForCommand(commandId);
		}
	}

	internal static int CalculateReplacementPoison(decimal damage)
	{
		return damage <= 0m
			? 0
			: Math.Max(1, (int)Math.Min(Math.Floor(damage / 3m), 999999999m));
	}

	internal static bool ShouldSkipDamageReplacement(Creature target, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse
			|| (IsPoisonDamageSignature(props, dealer, cardSource)
				&& target.GetPowerAmount<PoisonPower>() > 0m);
	}

	internal static bool IsPoisonDamageSignature(ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return dealer == null
			&& cardSource == null
			&& (props & ValueProp.Unblockable) != 0
			&& (props & ValueProp.Unpowered) != 0;
	}

	private void EnqueuePendingCompensation(long commandId, Creature target, decimal amount, Creature? dealer, CardModel? cardSource, bool shouldConsumeSlippery)
	{
		for (int i = _pendingCompensations.Count - 1; i >= 0; i--)
		{
			PendingCompensation pending = _pendingCompensations[i];
			if (pending.CommandId == commandId && pending.Target == target)
			{
				_pendingCompensations[i] = pending with
				{
					Amount = pending.Amount + amount,
					Dealer = dealer ?? pending.Dealer,
					CardSource = cardSource ?? pending.CardSource,
					ShouldConsumeSlippery = pending.ShouldConsumeSlippery || shouldConsumeSlippery
				};
				EffectsWithPendingCompensation.Add(this);
				return;
			}
		}

		_pendingCompensations.Add(new PendingCompensation(commandId, target, amount, dealer, cardSource, shouldConsumeSlippery));
		EffectsWithPendingCompensation.Add(this);
	}

	private bool TryTakePendingCompensation(long commandId, Creature target, out PendingCompensation? pending)
	{
		for (int i = 0; i < _pendingCompensations.Count; i++)
		{
			pending = _pendingCompensations[i];
			if (pending.CommandId != commandId || pending.Target != target)
			{
				continue;
			}

			_pendingCompensations.RemoveAt(i);
			RemoveFromPendingRegistryIfEmpty();
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

	private void ClearPendingCompensationsForCommand(long commandId)
	{
		_pendingCompensations.RemoveAll(pending => pending.CommandId == commandId);
		RemoveFromPendingRegistryIfEmpty();
	}

	private void ClearPendingCompensationsForEffect()
	{
		_pendingCompensations.Clear();
		EffectsWithPendingCompensation.Remove(this);
	}

	private void RemoveFromPendingRegistryIfEmpty()
	{
		if (_pendingCompensations.Count == 0)
		{
			EffectsWithPendingCompensation.Remove(this);
		}
	}

	private sealed record PendingCompensation(long CommandId, Creature Target, decimal Amount, Creature? Dealer, CardModel? CardSource, bool ShouldConsumeSlippery);
}

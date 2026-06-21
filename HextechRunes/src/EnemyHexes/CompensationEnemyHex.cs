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
			|| amount <= 0m)
		{
			return amount;
		}

		long commandId = HextechCombatHooks.CurrentActualDamageCommandId;
		if (commandId == 0L)
		{
			return amount;
		}

		int doom = Math.Min((int)Math.Floor(amount), 999999999);
		if (doom <= 0)
		{
			return amount;
		}

		EnqueuePendingCompensation(commandId, target, doom, dealer, cardSource);
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
		await PowerCmd.Apply<DoomPower>(target, compensation.Amount, compensation.Dealer ?? target, compensation.CardSource);
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

	private void EnqueuePendingCompensation(long commandId, Creature target, decimal amount, Creature? dealer, CardModel? cardSource)
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
					CardSource = cardSource ?? pending.CardSource
				};
				EffectsWithPendingCompensation.Add(this);
				return;
			}
		}

		_pendingCompensations.Add(new PendingCompensation(commandId, target, amount, dealer, cardSource));
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

	private sealed record PendingCompensation(long CommandId, Creature Target, decimal Amount, Creature? Dealer, CardModel? CardSource);
}

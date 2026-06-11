using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

public abstract partial class HextechRelicBase
{
	private HextechCombatState? _turnScopedCombatState;
	private int _turnScopedRoundNumber = -1;

	protected void EnsureTurnScopedStateCurrent(Action resetState)
	{
		HextechCombatState? combatState = Owner?.Creature.CombatState;
		if (combatState == null)
		{
			resetState();
			_turnScopedCombatState = null;
			_turnScopedRoundNumber = -1;
			return;
		}

		if (!ReferenceEquals(_turnScopedCombatState, combatState)
			|| _turnScopedRoundNumber != combatState.RoundNumber)
		{
			resetState();
			UpdateTurnScopedStateIdentity(combatState);
		}
	}

	protected void UpdateTurnScopedStateIdentity(HextechCombatState? combatState = null)
	{
		combatState ??= Owner?.Creature.CombatState;
		_turnScopedCombatState = combatState;
		_turnScopedRoundNumber = combatState?.RoundNumber ?? -1;
	}

	protected bool ShouldUseNetworkCombatHistory()
	{
		return IsNetworkMultiplayer()
			&& CombatManager.Instance?.IsInProgress == true
			&& Owner != null;
	}

	protected bool TryGetNetworkTurnProcCount(string procKey, out int count)
	{
		count = 0;
		if (!ShouldUseNetworkCombatHistory()
			|| Owner == null
			|| Owner.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is not HextechMayhemModifier modifier)
		{
			return false;
		}

		count = modifier.GetPlayerRuneProcsThisTurn(Owner, procKey);
		return true;
	}

	protected bool HasTurnProcReachedLimit(string procKey, int localCount, int maxPerTurn)
	{
		if (TryGetNetworkTurnProcCount(procKey, out int networkCount))
		{
			return networkCount >= maxPerTurn;
		}

		return localCount >= maxPerTurn;
	}

	protected int GetTurnProcCount(string procKey, int localCount)
	{
		return TryGetNetworkTurnProcCount(procKey, out int networkCount)
			? networkCount
			: localCount;
	}

	protected bool HasTurnProcTriggered(string procKey, bool localTriggered)
	{
		return HasTurnProcReachedLimit(procKey, localTriggered ? 1 : 0, 1);
	}

	protected bool TryConsumeTurnProc(string procKey, ref int localCount, int maxPerTurn)
	{
		if (maxPerTurn <= 0)
		{
			return false;
		}

		if (ShouldUseNetworkCombatHistory()
			&& Owner != null
			&& Owner.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier)
		{
			if (!modifier.TryConsumePlayerRuneProcThisTurn(Owner, procKey, maxPerTurn))
			{
				return false;
			}

			localCount = modifier.GetPlayerRuneProcsThisTurn(Owner, procKey);
			UpdateTurnScopedStateIdentity();
			InvokeDisplayAmountChanged();
			return true;
		}

		if (localCount >= maxPerTurn)
		{
			return false;
		}

		localCount++;
		UpdateTurnScopedStateIdentity();
		InvokeDisplayAmountChanged();
		return true;
	}

	protected bool TryConsumeTurnProc(string procKey, ref bool localTriggered)
	{
		int localCount = localTriggered ? 1 : 0;
		if (!TryConsumeTurnProc(procKey, ref localCount, 1))
		{
			return false;
		}

		localTriggered = true;
		return true;
	}

	protected int ConsumeCombatProcOrdinal(string procKey, ref int localCount)
	{
		if (ShouldUseNetworkCombatHistory()
			&& Owner != null
			&& Owner.RunState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier)
		{
			int ordinal = modifier.ConsumePlayerRuneProcInCombat(Owner, procKey);
			localCount = ordinal + 1;
			return ordinal;
		}

		int localOrdinal = localCount;
		localCount++;
		return localOrdinal;
	}

	protected void FlashDeferred(IEnumerable<Creature>? targets = null)
	{
		Creature[] targetArray = targets?.ToArray() ?? Array.Empty<Creature>();
		Callable.From(() => Flash(targetArray)).CallDeferred();
	}
}

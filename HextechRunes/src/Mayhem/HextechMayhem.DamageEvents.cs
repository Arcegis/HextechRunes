namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target.Side == CombatSide.Enemy && target.CombatState?.RunState == RunState)
		{
			await HextechEnemyHexDispatcher.ForEachActive(
				this,
				(effect, context) => effect.AfterEnemyDamageReceivedAny(context, target, result, dealer, cardSource));
		}

		if (!TryGetDamagedEnemy(target, result, out uint combatId))
		{
			return;
		}

		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterEnemyDamageReceived(context, target, combatId, result, dealer, cardSource));

		if (!HextechEnemyTriggerGuard.ShouldSuppressDuplicateEnemyThresholdTrigger(_combatTracking, target, result, dealer, cardSource)
			&& IsBelowEnemyHealthThreshold(target))
		{
			await HextechEnemyHexDispatcher.ForEachActive(
				this,
				(effect, context) => effect.AfterEnemyHealthThreshold(context, target, combatId));
		}
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (dealer?.Side != CombatSide.Enemy || dealer.CombatState?.RunState != RunState || !target.IsAlive)
		{
			return;
		}

		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterEnemyDamageGivenImmediate(context, dealer, result, target, cardSource));

		if (result.UnblockedDamage <= 0 || target.Side != CombatSide.Player)
		{
			return;
		}

		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterEnemyDamageGivenPlayerHit(context, dealer, target));
	}

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterCurrentHpChanged(context, creature, delta));
	}

	private static bool TryGetDamagedEnemy(Creature target, DamageResult result, out uint combatId)
	{
		combatId = 0;
		if (target.Side != CombatSide.Enemy || result.UnblockedDamage <= 0 || target.CombatId == null)
		{
			return false;
		}

		combatId = target.CombatId.Value;
		return true;
	}

	private static bool IsBelowEnemyHealthThreshold(Creature target)
	{
		return target.CurrentHp < target.MaxHp * EscapePlanHealthThresholdPercent;
	}
}

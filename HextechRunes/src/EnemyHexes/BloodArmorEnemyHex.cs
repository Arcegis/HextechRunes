namespace HextechRunes;

internal sealed class BloodArmorEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BloodArmor;

	internal override async Task AfterCurrentHpChanged(HextechEnemyHexContext context, Creature creature, decimal delta)
	{
		if (delta >= 0m
			|| creature.Side != CombatSide.Enemy
			|| creature.IsDead
			|| creature.CombatId == null
			|| creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState
			|| combatState.CurrentSide != CombatSide.Player)
		{
			return;
		}

		int hpLoss = (int)Math.Floor(-delta);
		if (hpLoss <= 0)
		{
			return;
		}

		uint combatId = creature.CombatId.Value;
		int hpLossPerPlating = context.TierValue(Kind, 12, 10, 8);
		int accumulated = context.Tracking.BloodArmorHpLossThisPlayerTurn.GetValueOrDefault(combatId, 0) + hpLoss;
		int plating = accumulated / hpLossPerPlating;
		context.Tracking.BloodArmorHpLossThisPlayerTurn[combatId] = accumulated % hpLossPerPlating;
		if (plating > 0)
		{
			await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(creature, plating, creature, null);
		}
	}
}

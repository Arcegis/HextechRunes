namespace HextechRunes;

public sealed class QuantumComputingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamagePercent", 10m),
		new DamageVar(10m, ValueProp.Unpowered),
		new DynamicVar("HealPercent", 10m)
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		int round = Owner.Creature.CombatState.RoundNumber;
		if (round <= 0 || round % 2 != 0)
		{
			return;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		// 表现:蓝紫量子光柱逐敌贯穿+吸血数据流回流(纯表现层);
		// 逻辑等待让首柱落点与首敌伤害对齐,后续逐敌结算的标准尾巴与柱间节拍一致。
		HextechCombatVfx.QuantumPulse(Owner.Creature, enemies);
		await Cmd.CustomScaledWait(0.42f, 0.55f);
		int totalDamage = 0;
		foreach (Creature enemy in enemies)
		{
			decimal damage = DynamicVars.Damage.BaseValue + Math.Floor(enemy.MaxHp * DynamicVars["DamagePercent"].BaseValue / 100m);
			IEnumerable<DamageResult> results = await HextechGameApiCompat.Damage(choiceContext, enemy, damage, ValueProp.Unpowered, Owner.Creature, null);
			totalDamage += results.Sum(static result => result.UnblockedDamage);
		}

		int heal = FloorToInt(totalDamage * DynamicVars["HealPercent"].BaseValue / 100m);
		if (heal > 0 && !Owner.Creature.IsDead)
		{
			await CreatureCmd.Heal(Owner.Creature, heal);
		}
	}
}

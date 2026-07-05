namespace HextechRunes;

public sealed class MiseryRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(-1m),
		new PowerVar<DexterityPower>(-1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		// 只吸取当前生命值最高的敌人:HittableEnemies 顺序两端一致,同血量取靠前者保证联机一致。
		Creature target = enemies
			.OrderByDescending(static enemy => enemy.CurrentHp)
			.First();
		List<Creature> flashTargets = [target, Owner.Creature];
		FlashDeferred(flashTargets);
		await PowerCmd.Apply<StrengthPower>(target, DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(target, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, -DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, -DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
	}
}

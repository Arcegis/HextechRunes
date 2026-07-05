namespace HextechRunes;

public sealed class QueenRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FrailPower>(2m),
		new PowerVar<WeakPower>(2m),
		new PowerVar<VulnerablePower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FrailPower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		// 只压制当前生命值最高的敌人:HittableEnemies 顺序两端一致,同血量取靠前者保证联机一致。
		Creature? target = combatState.HittableEnemies
			.OrderByDescending(static enemy => enemy.CurrentHp)
			.FirstOrDefault();
		if (target == null)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FrailPower>(target, DynamicVars["FrailPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<WeakPower>(target, DynamicVars.Weak.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<VulnerablePower>(target, DynamicVars.Vulnerable.BaseValue, Owner.Creature, null);
	}
}

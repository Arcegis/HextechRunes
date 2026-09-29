namespace HextechRunes;

public sealed class DeathWarrantRune : DrawThresholdRuneBase
{
	internal const int CardsNeeded = 8;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCardsDrawnThisCombat
	{
		get => SavedDrawProgress;
		set => SavedDrawProgress = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CardsNeeded", CardsNeeded)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PoisonPower>()
	];

	public override bool IsAvailableForPlayer(Player player) => IsSilentPlayer(player);

	protected override async Task ApplyDrawThresholdReward()
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		PoisonPower[] poisonPowers = combatState.HittableEnemies
			.Where(static enemy => enemy.IsAlive)
			.Select(static enemy => enemy.GetPower<PoisonPower>())
			.Where(static power => power is { Amount: > 0 })
			.Cast<PoisonPower>()
			.ToArray();
		if (poisonPowers.Length == 0)
		{
			return;
		}

		Flash(poisonPowers.Select(static power => power.Owner));
		foreach (PoisonPower poison in poisonPowers)
		{
			await TriggerPoisonCompat(poison, combatState);
		}
	}

	internal static Task TriggerPoisonCompat(PoisonPower poison, HextechCombatState combatState)
	{
		// 0.110.0 才公开 PoisonPower.Trigger；调用两版本共有的回合触发入口可保持伤害、
		// 催化剂段数、层数递减以及病入膏肓拦截逻辑与原版一致。
		return poison.AfterSideTurnStart(poison.Owner.Side, [poison.Owner], combatState);
	}
}

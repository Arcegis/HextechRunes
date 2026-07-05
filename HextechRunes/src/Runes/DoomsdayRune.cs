namespace HextechRunes;

public sealed class DoomsdayRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DoomPercent", 5m),
		new DynamicVar("MinimumDoom", 5m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<DoomPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

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

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			decimal doom = Math.Max(
				DynamicVars["MinimumDoom"].BaseValue,
				decimal.Floor(enemy.MaxHp * DynamicVars["DoomPercent"].BaseValue / 100m));
			await PowerCmd.Apply<DoomPower>(enemy, doom, Owner.Creature, null);
		}
	}
}

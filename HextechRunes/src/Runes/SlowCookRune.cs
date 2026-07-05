namespace HextechRunes;

public sealed class SlowCookRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BurnPercent", 5m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return;
		}

		int burnAmount = FloorToInt(player.Creature.MaxHp * (DynamicVars["BurnPercent"].BaseValue / 100m));
		if (burnAmount <= 0)
		{
			return;
		}

		HextechCombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			await PowerCmd.Apply<HextechBurnPower>(enemy, burnAmount, player.Creature, null);
		}
	}
}

namespace HextechRunes;

public sealed class FrostWraithRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnsNeeded", 3m),
		new PowerVar<SlowPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<SlowPower>()
	];

	// 额外回合不推进 RoundNumber 且回合开始 hook 会重入,周期触发按 RoundNumber 防重。
	private int _lastProcRound = -1;

	public override async Task BeforeCombatStart()
	{
		_lastProcRound = -1;
		if (Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		await ApplySlow(combatState);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RoundNumber <= 1
			|| (combatState.RoundNumber - 1) % DynamicVars["TurnsNeeded"].IntValue != 0
			|| _lastProcRound == combatState.RoundNumber)
		{
			return;
		}

		_lastProcRound = combatState.RoundNumber;
		await ApplySlow(combatState);
	}

	private async Task ApplySlow(HextechCombatState combatState)
	{
		IReadOnlyList<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<HextechTemporarySlowPower>(enemies, DynamicVars["SlowPower"].BaseValue, Owner.Creature, null);
	}
}

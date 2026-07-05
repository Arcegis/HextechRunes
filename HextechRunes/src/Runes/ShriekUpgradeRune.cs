namespace HextechRunes;

public sealed class ShriekUpgradeRune : CardUpgradeRuneBase<PiercingWail>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<PiercingWail>(),
		HoverTipFactory.FromPower<StrengthPower>()
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not PiercingWail
			|| Owner.Creature.CombatState is not { } combatState)
		{
			return;
		}

		List<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			await PowerCmd.Apply<StrengthPower>(enemy, -DynamicVars["StrengthPower"].BaseValue, Owner.Creature, cardPlay.Card);
		}
	}
}

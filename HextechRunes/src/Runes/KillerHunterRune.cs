namespace HextechRunes;

public sealed class KillerHunterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TemporaryStatLoss", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedCard(cardPlay.Card) || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return;
		}

		IReadOnlyList<Creature> enemies = Owner.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		await PowerCmd.Apply<HextechTemporaryStrengthLossPower>(enemies, DynamicVars["TemporaryStatLoss"].BaseValue, Owner.Creature, cardPlay.Card);
		await PowerCmd.Apply<HextechTemporaryDexterityLossPower>(enemies, DynamicVars["TemporaryStatLoss"].BaseValue, Owner.Creature, cardPlay.Card);
	}
}

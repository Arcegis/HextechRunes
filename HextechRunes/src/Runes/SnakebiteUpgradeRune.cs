namespace HextechRunes;

public sealed class SnakebiteUpgradeRune : CardUpgradeRuneBase<Snakebite>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side)
		{
			return Task.CompletedTask;
		}

		List<CardModel> retainedSnakebites = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Owner == Owner && card is Snakebite && card.ShouldRetainThisTurn)
			.ToList();
		if (retainedSnakebites.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in retainedSnakebites)
		{
			card.BaseReplayCount += DynamicVars["Replays"].IntValue;
			CardCmd.Preview(card);
		}

		return Task.CompletedTask;
	}
}

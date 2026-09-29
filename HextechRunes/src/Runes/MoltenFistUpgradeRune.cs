namespace HextechRunes;

public sealed class MoltenFistUpgradeRune : CardUpgradeRuneBase<MoltenFist>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<MoltenFist>(),
		HoverTipFactory.FromCard<Dominate>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not MoltenFist
			|| cardPlay.Card.CombatState is not { } combatState)
		{
			return;
		}

		Flash();
		CardModel dominate = combatState.CreateCard<Dominate>(Owner);
		await HextechCardGeneration.AddGeneratedCardToCombat(dominate, PileType.Hand, addedByPlayer: true);
	}
}

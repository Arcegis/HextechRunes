namespace HextechRunes;

public sealed class KnowThyPlaceUpgradeRune : CardUpgradeRuneBase<KnowThyPlace>
{
	private const decimal ExtraStatLoss = 1m;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not KnowThyPlace
			|| cardPlay.Target == null
			|| cardPlay.Target.Side == Owner.Creature.Side)
		{
			return;
		}

		Flash([cardPlay.Target]);
		await PowerCmd.Apply<StrengthPower>(cardPlay.Target, -ExtraStatLoss, Owner.Creature, cardPlay.Card);
		await PowerCmd.Apply<DexterityPower>(cardPlay.Target, -ExtraStatLoss, Owner.Creature, cardPlay.Card);
	}
}

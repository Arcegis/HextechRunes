namespace HextechRunes;

public sealed class SpinToWinRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<DrawCardsNextTurnPower>(),
		HoverTipFactory.FromPower<EnergyNextTurnPower>(),
		HoverTipFactory.FromPower<SummonNextTurnPower>(),
		HoverTipFactory.FromPower<StarNextTurnPower>()
	];

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return ConvertDelayedResource(choiceContext, power, amount);
	}

	private async Task ConvertDelayedResource(PlayerChoiceContext choiceContext, PowerModel power, decimal amount)
	{
		if (Owner.Creature.IsDead
			|| power.Owner != Owner.Creature
			|| amount <= 0m
			|| power.Amount <= 0)
		{
			return;
		}

		decimal pendingAmount = power.Amount;
		switch (power)
		{
			case DrawCardsNextTurnPower:
				await CardPileCmd.Draw(choiceContext, pendingAmount, Owner, fromHandDraw: false);
				break;
			case EnergyNextTurnPower:
				await PlayerCmd.GainEnergy(pendingAmount, Owner);
				break;
			case SummonNextTurnPower:
				await OstyCmd.Summon(choiceContext, Owner, pendingAmount, power);
				break;
			case StarNextTurnPower:
				await PlayerCmd.GainStars(pendingAmount, Owner);
				break;
			default:
				return;
		}

		await PowerCmd.Remove(power);
		Flash();
	}
}

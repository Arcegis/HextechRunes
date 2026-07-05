namespace HextechRunes;

public sealed class BorrowedTimeUpgradeRune : CardUpgradeRuneBase<BorrowedTime>
{
	private decimal _borrowedTimeBeforePlay;
	private bool _shouldCleanBorrowedTime;

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		_shouldCleanBorrowedTime = cardPlay.Card.Owner == Owner && cardPlay.Card is BorrowedTime;
		_borrowedTimeBeforePlay = _shouldCleanBorrowedTime && Owner != null
			? Owner.Creature.GetPowerAmount<BorrowedTimePower>()
			: 0m;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!_shouldCleanBorrowedTime || Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Card is not BorrowedTime)
		{
			return;
		}

		_shouldCleanBorrowedTime = false;
		BorrowedTimePower? borrowedTime = Owner.Creature.GetPower<BorrowedTimePower>();
		decimal excess = (borrowedTime?.Amount ?? 0m) - _borrowedTimeBeforePlay;
		if (excess <= 0m)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<BorrowedTimePower>(Owner.Creature, -excess, Owner.Creature, cardPlay.Card, silent: true);
	}
}

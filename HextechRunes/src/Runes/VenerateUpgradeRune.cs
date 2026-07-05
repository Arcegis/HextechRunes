namespace HextechRunes;

public sealed class VenerateUpgradeRune : CardUpgradeRuneBase<Venerate>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Venerate
			|| Owner.PlayerCombatState == null)
		{
			return;
		}

		int stars = Owner.PlayerCombatState.Stars;
		if (stars <= 0)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainStars(stars, Owner);
	}
}

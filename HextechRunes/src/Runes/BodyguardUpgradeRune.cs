namespace HextechRunes;

public sealed class BodyguardUpgradeRune : CardUpgradeRuneBase<Bodyguard>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Bodyguard
			|| !IsNecrobinderPlayer(Owner)
			|| Owner.Osty is not { IsAlive: true } osty)
		{
			return;
		}

		decimal heal = osty.MaxHp - osty.CurrentHp;
		if (heal <= 0m)
		{
			return;
		}

		Flash([osty]);
		await CreatureCmd.Heal(osty, heal);
	}
}

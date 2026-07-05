namespace HextechRunes;

public sealed class HotfixUpgradeRune : CardUpgradeRuneBase<Hotfix>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Hotfix
			|| Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, 1m, Owner.Creature, cardPlay.Card);
	}
}

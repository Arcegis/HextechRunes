namespace HextechRunes;

public sealed class StarlightSplendorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StarsVar(2)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		decimal stars = Owner.Creature.CombatState.RoundNumber * DynamicVars.Stars.BaseValue;
		if (stars <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainStars(stars, Owner);
	}
}

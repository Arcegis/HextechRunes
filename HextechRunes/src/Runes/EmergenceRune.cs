namespace HextechRunes;

public sealed class EmergenceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbCount", 2m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		for (int i = 0; i < DynamicVars["OrbCount"].IntValue; i++)
		{
			OrbModel orb = HextechStableRandom.CreateOrb(
				(RunState)Owner.RunState,
				Owner,
				"emergence-turn-start-orb",
				i,
				Owner.Creature.CombatState?.RoundNumber ?? -1);
			await OrbCmd.Channel(choiceContext, orb, Owner);
		}
	}
}

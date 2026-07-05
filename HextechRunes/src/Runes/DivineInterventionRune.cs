namespace HextechRunes;

public sealed class DivineInterventionRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnsNeeded", 3m),
		new PowerVar<IntangiblePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNetworkMultiplayer();
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RoundNumber <= 1
			|| combatState.RoundNumber % DynamicVars["TurnsNeeded"].IntValue != 0)
		{
			return;
		}

		IReadOnlyList<Creature> players = combatState.Players
			.Where(static combatPlayer => combatPlayer.Creature.IsAlive)
			.Select(static combatPlayer => combatPlayer.Creature)
			.ToList();
		if (players.Count == 0)
		{
			return;
		}

		Flash(players);
		HextechCombatVfx.DivinePulse(players);
		await PowerCmd.Apply<IntangiblePower>(players, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
	}
}

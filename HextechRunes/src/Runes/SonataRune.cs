namespace HextechRunes;

public sealed class SonataRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new HealVar(1m),
		new BlockVar(2m, ValueProp.Unpowered)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNetworkMultiplayer();
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner
			|| Owner.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<Player> players = combatState.Players
			.Where(static combatPlayer => combatPlayer.Creature.IsAlive)
			.ToList();
		if (players.Count == 0)
		{
			return;
		}

		FlashDeferred(players.Select(static combatPlayer => combatPlayer.Creature));
		if (combatState.RoundNumber % 2 == 1)
		{
			foreach (Player combatPlayer in players)
			{
				await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, combatPlayer, fromHandDraw: false);
			}

			return;
		}

		foreach (Player combatPlayer in players)
		{
			await CreatureCmd.Heal(combatPlayer.Creature, DynamicVars.Heal.BaseValue);
			await CreatureCmd.GainBlock(combatPlayer.Creature, DynamicVars.Block, null);
		}
	}
}

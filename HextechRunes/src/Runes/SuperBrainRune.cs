namespace HextechRunes;

public sealed class SuperBrainRune : HextechRelicBase
{
	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		int plating = Owner.Deck.Cards.Count / 3;
		if (plating <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<PlatingPower>(Owner.Creature, plating, Owner.Creature, null);
	}
}

namespace HextechRunes;

public sealed class SturdyRune : HextechRelicBase
{
	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return Task.CompletedTask;
		}

		decimal percent = player.Creature.CurrentHp < player.Creature.MaxHp * 0.5m ? 0.04m : 0.02m;
		int healAmount = Math.Max(1, FloorToInt(player.Creature.MaxHp * percent));
		Flash();
		return CreatureCmd.Heal(player.Creature, healAmount);
	}
}

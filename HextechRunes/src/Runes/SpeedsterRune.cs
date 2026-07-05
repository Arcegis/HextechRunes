namespace HextechRunes;

public sealed class SpeedsterRune : HextechRelicBase
{
	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + (player.PlayerCombatState?.MaxEnergy ?? 0) / 2;
	}
}

namespace HextechRunes;

public sealed class JuggernautUpgradeRune : CardUpgradeRuneBase<Juggernaut>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}
}

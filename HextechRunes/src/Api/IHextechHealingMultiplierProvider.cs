namespace HextechRunes;

public interface IHextechHealingMultiplierProvider
{
	decimal ModifyHealingMultiplicative(Player player, Creature creature, decimal amount);
}

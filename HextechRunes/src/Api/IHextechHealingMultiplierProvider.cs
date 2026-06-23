using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

public interface IHextechHealingMultiplierProvider
{
	decimal ModifyHealingMultiplicative(Player player, Creature creature, decimal amount);
}

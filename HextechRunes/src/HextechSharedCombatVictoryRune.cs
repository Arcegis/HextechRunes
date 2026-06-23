using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

public interface IHextechSharedCombatVictoryRune
{
	Task ApplySharedCombatVictory(CombatRoom room);
}

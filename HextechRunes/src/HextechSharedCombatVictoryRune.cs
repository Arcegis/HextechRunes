using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

internal interface IHextechSharedCombatVictoryRune
{
	Task ApplySharedCombatVictory(CombatRoom room);
}

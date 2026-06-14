using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HextechRunes;

internal static class HextechMonsterSustainHelper
{
	public static decimal GetProteinShakeSustainMultiplier(Creature creature)
	{
		decimal bonusPercent = Math.Min(100m, Math.Floor(creature.MaxHp / 5m));
		return 1m + bonusPercent / 100m;
	}
}

using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;

namespace HextechRunes;

public sealed class HattrickRune : HextechRelicBase
{
	public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward)
	{
		if (Owner == null || player != Owner || Owner.Creature.IsDead)
		{
			return false;
		}

		bool canContinue = cardReward.Cards.Count() > 1;
		if (canContinue)
		{
			Flash();
		}

		return canContinue;
	}
}

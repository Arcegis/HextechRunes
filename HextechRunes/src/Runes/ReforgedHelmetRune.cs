using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class ReforgedHelmetRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (Owner == null || target != Owner.Creature || canonicalPower is not StrengthPower || amount <= 0m)
		{
			return false;
		}

		modifiedAmount *= 2m;
		return true;
	}

	public override Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
		Flash();
		return Task.CompletedTask;
	}
}

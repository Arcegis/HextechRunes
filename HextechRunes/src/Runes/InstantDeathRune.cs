using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class InstantDeathRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<DoomPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (power is DoomPower doomPower && amount > 0m)
		{
			await KillIfDoomExceedsHp(doomPower.Owner);
		}
	}

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (delta < 0m)
		{
			await KillIfDoomExceedsHp(creature);
		}
	}

	private async Task KillIfDoomExceedsHp(Creature creature)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| creature.Side != CombatSide.Enemy
			|| !creature.IsAlive
			|| creature.GetPowerAmount<DoomPower>() <= creature.CurrentHp)
		{
			return;
		}

		Flash([creature]);
		await DoomPower.DoomKill([creature]);
	}
}

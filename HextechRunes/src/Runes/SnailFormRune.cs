using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HextechRunes;

public sealed class SnailFormRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("InitialSlow", -100m),
		new DynamicVar("TurnStartSlow", -100m),
		new DynamicVar("CardSlowGain", HextechPlayerSlowPower.CardPlaySlowIncrease)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechPlayerSlowPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await HextechPowerCmdCompat.Apply<HextechPlayerSlowPower>(Owner.Creature, DynamicVars["InitialSlow"].BaseValue, Owner.Creature, null);
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return;
		}

		decimal current = Owner.Creature.GetPowerAmount<HextechPlayerSlowPower>();
		decimal target = DynamicVars["TurnStartSlow"].BaseValue;
		decimal delta = target - current;
		if (delta == 0m)
		{
			return;
		}

		Flash();
		await HextechPowerCmdCompat.Apply<HextechPlayerSlowPower>(Owner.Creature, delta, Owner.Creature, null);
	}
}

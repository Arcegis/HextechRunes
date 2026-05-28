using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

public sealed class OverlordBloodArmorRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPerStrength", 40m),
		new PowerVar<StrengthPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		int strength = FloorToInt(Owner.Creature.MaxHp / DynamicVars["MaxHpPerStrength"].BaseValue);
		if (strength <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<StrengthPower>(Owner.Creature, strength * DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}

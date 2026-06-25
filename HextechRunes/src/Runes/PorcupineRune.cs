using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class PorcupineRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("AttackThorns", 2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ThornsPower>()
	];

	public override async Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| Owner.Creature.IsDead
			|| !HextechSts2Compat.IsPoweredAttack(props)
			|| result.TotalDamage <= 0m)
		{
			return;
		}

		decimal thorns = DynamicVars["AttackThorns"].BaseValue;

		Flash();
		await PowerCmd.Apply<ThornsPower>(Owner.Creature, thorns, Owner.Creature, null);
	}
}

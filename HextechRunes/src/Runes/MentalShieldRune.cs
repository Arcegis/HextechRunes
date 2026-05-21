using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class MentalShieldRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(2m, ValueProp.Unpowered)
	];

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side)
		{
			return Task.CompletedTask;
		}

		int handCount = PileType.Hand.GetPile(Owner).Cards.Count;
		if (handCount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue * handCount, ValueProp.Unpowered, null);
	}
}

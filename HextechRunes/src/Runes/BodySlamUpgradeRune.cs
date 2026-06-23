using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class BodySlamUpgradeRune : CardUpgradeRuneBase<BodySlam>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 2m)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsBodySlam(cardSource) || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		return DynamicVars["DamageMultiplier"].BaseValue;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || Owner.Creature.IsDead || cardPlay.Card.Owner != Owner || !IsBodySlam(cardPlay.Card))
		{
			return;
		}

		decimal block = Owner.Creature.Block;
		if (block <= 0m)
		{
			return;
		}

		Flash();
		await CreatureCmd.LoseBlock(Owner.Creature, block);
	}

	private static bool IsBodySlam(CardModel? card)
	{
		return card is BodySlam;
	}
}

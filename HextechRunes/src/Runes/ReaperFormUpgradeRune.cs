using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ReaperFormUpgradeRune : CardUpgradeRuneBase<ReaperForm>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DoomPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<ReaperForm>(),
		HoverTipFactory.FromPower<DoomPower>()
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.GetPowerAmount<ReaperFormPower>() <= 0m
			|| target.Side != CombatSide.Enemy
			|| result.TotalDamage <= 0m
			|| !IsAttackDamageForRuneEffects(props, cardSource)
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		Flash([target]);
		await PowerCmd.Apply<DoomPower>(target, DynamicVars["DoomPower"].BaseValue, Owner.Creature, cardSource);
	}
}

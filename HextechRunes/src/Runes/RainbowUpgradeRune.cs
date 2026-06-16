using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace HextechRunes;

public sealed class RainbowUpgradeRune : CardUpgradeRuneBase<Rainbow>
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar("CostReduction", 1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Rainbow>(),
		HoverTipFactory.FromOrb<LightningOrb>(),
		HoverTipFactory.FromOrb<FrostOrb>(),
		HoverTipFactory.FromOrb<DarkOrb>(),
		HoverTipFactory.FromOrb<GlassOrb>(),
		HoverTipFactory.FromOrb<PlasmaOrb>()
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card.Owner != Owner || card is not Rainbow || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = Math.Max(0m, originalCost - DynamicVars["CostReduction"].BaseValue);
		return true;
	}

	internal static bool ShouldUseUpgradedPlay(CardModel card)
	{
		return card is Rainbow && card.Owner?.GetRelic<RainbowUpgradeRune>() != null;
	}

	internal static async Task PlayUpgraded(PlayerChoiceContext choiceContext, Rainbow card, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);

		card.Owner.GetRelic<RainbowUpgradeRune>()?.Flash();
		await OrbCmd.Channel<LightningOrb>(choiceContext, card.Owner);
		await OrbCmd.Channel<FrostOrb>(choiceContext, card.Owner);
		await OrbCmd.Channel<DarkOrb>(choiceContext, card.Owner);
		await OrbCmd.Channel<GlassOrb>(choiceContext, card.Owner);
		await OrbCmd.Channel<PlasmaOrb>(choiceContext, card.Owner);
	}
}

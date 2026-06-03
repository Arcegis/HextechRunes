using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace HextechRunes;

public sealed class GrandFinaleUpgradeRune : CardUpgradeRuneBase<GrandFinale>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	internal static bool AllowsPlaying(CardModel card)
	{
		return card is GrandFinale && card.Owner?.GetRelic<GrandFinaleUpgradeRune>() != null;
	}

	internal static async Task PlayUpgradedSafely(PlayerChoiceContext choiceContext, GrandFinale card)
	{
		var combatState = card.CombatState;
		if (combatState == null)
		{
			return;
		}

		await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
			.FromCard(card)
			.TargetingAllOpponents(combatState)
			.WithHitVfxNode(NGrandFinaleImpactVfx.Create)
			.WithHitFx(null, null, "blunt_attack.mp3")
			.Execute(choiceContext);
	}
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class CrashLandingUpgradeRune : CardUpgradeRuneBase<CrashLanding>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<CrashLanding>(),
		HoverTipFactory.FromCard<CollisionCourse>()
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	internal static bool ShouldUseUpgradedPlay(CardModel card)
	{
		return card is CrashLanding && card.Owner?.GetRelic<CrashLandingUpgradeRune>() != null;
	}

	internal static async Task PlayUpgraded(PlayerChoiceContext choiceContext, CrashLanding card)
	{
		var combatState = card.CombatState;
		if (combatState == null)
		{
			return;
		}

		card.Owner.GetRelic<CrashLandingUpgradeRune>()?.Flash();
		await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
			.FromCardCompat(card)
			.TargetingAllOpponents(combatState)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.WithHitVfxSpawnedAtBase()
			.Execute(choiceContext);

		int cardsToAdd = CardPile.MaxCardsInHand - CardPile.GetCards(card.Owner, PileType.Hand).Count();
		if (cardsToAdd <= 0)
		{
			return;
		}

		List<CardModel> collisionCourses = new();
		for (int i = 0; i < cardsToAdd; i++)
		{
			collisionCourses.Add(combatState.CreateCard<CollisionCourse>(card.Owner));
		}

		await CardPileCmd.AddGeneratedCardsToCombat(collisionCourses, PileType.Hand, card.Owner);
	}
}

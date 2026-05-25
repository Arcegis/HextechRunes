using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class PrimitiveMadnessRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<GiantRock>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> transformableCards = Owner.Deck.Cards
			.Where(static card => card.IsTransformable)
			.ToList();
		if (transformableCards.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, transformableCards.Count)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			},
			static card => card.IsTransformable);

		List<CardTransformation> transformations = selected
			.Select(card => CardTransformUpgradeHelper.CreateFixedReplacementTransformation(
				card,
				Owner.RunState.CreateCard<GiantRock>(Owner)))
			.ToList();
		if (transformations.Count > 0)
		{
			Flash();
			await CardCmd.Transform(transformations, null, CardPreviewStyle.GridLayout);
		}
	}
}

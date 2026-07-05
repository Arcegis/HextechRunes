using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

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

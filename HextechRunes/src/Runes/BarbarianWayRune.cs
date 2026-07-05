using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class BarbarianWayRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Claw>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> transformableCards = Owner.Deck.Cards
			.Where(CanTransformToClaw)
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
			CanTransformToClaw);

		List<CardTransformation> transformations = selected
			.Select(card => CardTransformUpgradeHelper.CreateFixedReplacementTransformation(
				card,
				Owner.RunState.CreateCard<Claw>(Owner)))
			.ToList();
		if (transformations.Count == 0)
		{
			return;
		}

		Flash();
		await CardCmd.Transform(transformations, null, CardPreviewStyle.GridLayout);
	}

	private static bool CanTransformToClaw(CardModel card)
	{
		return card.Type == CardType.Attack && card.IsTransformable;
	}
}

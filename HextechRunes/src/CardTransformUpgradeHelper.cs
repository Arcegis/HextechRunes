using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;

namespace HextechRunes;

internal static class CardTransformUpgradeHelper
{
	public static CardTransformation CreateRandomOptionTransformation(
		CardModel original,
		IEnumerable<CardModel> replacementOptions,
		Rng rng)
	{
		CardModel replacement = CardFactory.CreateRandomCardForTransform(
			original,
			replacementOptions,
			original.CombatState != null,
			rng);
		PreserveUpgradeLevel(original, replacement);
		return new CardTransformation(original, replacement);
	}

	public static CardTransformation CreateFixedReplacementTransformation(CardModel original, CardModel replacement)
	{
		PreserveUpgradeLevel(original, replacement);
		return new CardTransformation(original, replacement);
	}

	public static void PreserveUpgradeLevel(CardModel original, CardModel replacement)
	{
		int targetUpgradeLevel = Math.Min(original.CurrentUpgradeLevel, replacement.MaxUpgradeLevel);
		while (replacement.CurrentUpgradeLevel < targetUpgradeLevel && replacement.IsUpgradable)
		{
			CardCmd.Upgrade(replacement, CardPreviewStyle.None);
		}
	}
}

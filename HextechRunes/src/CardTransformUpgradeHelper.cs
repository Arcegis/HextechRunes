using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

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

	public static CardTransformation CreateStableOptionTransformation(
		CardModel original,
		IEnumerable<CardModel> replacementOptions,
		RunState runState,
		string source,
		int ordinal,
		params string?[] saltParts)
	{
		CardModel[] filteredOptions = GetStableTransformationOptions(original, replacementOptions, original.CombatState != null);
		CardModel canonicalReplacement = HextechStableRandom.Pick(
			filteredOptions,
			runState,
			HextechStableRandom.CardKey,
			BuildStableTransformSalt(original, source, ordinal, saltParts));
		CardModel replacement = original.CardScope!.CreateCard(canonicalReplacement, original.Owner!);
		PreserveUpgradeLevel(original, replacement);
		return new CardTransformation(original, replacement);
	}

	public static Task<CardPileAddResult?> TransformToStableRandom(
		CardModel original,
		RunState runState,
		string source,
		int ordinal,
		CardPreviewStyle style = CardPreviewStyle.HorizontalLayout,
		params string?[] saltParts)
	{
		CardTransformation transformation = CreateStableOptionTransformation(
			original,
			CardFactory.GetDefaultTransformationOptions(original, original.CombatState != null),
			runState,
			source,
			ordinal,
			saltParts);
		return CardCmd.Transform(transformation.Original, transformation.Replacement!, style);
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

	private static string?[] BuildStableTransformSalt(CardModel original, string source, int ordinal, params string?[] saltParts)
	{
		string?[] result = new string?[saltParts.Length + 5];
		result[0] = source;
		result[1] = original.Owner == null ? "owner:none" : HextechStableRandom.PlayerKey(original.Owner);
		result[2] = HextechStableRandom.CardKey(original);
		result[3] = ordinal.ToString();
		result[4] = HextechStableRandom.CardActionKey(original);
		Array.Copy(saltParts, 0, result, 5, saltParts.Length);
		return result;
	}

	private static CardModel[] GetStableTransformationOptions(CardModel original, IEnumerable<CardModel> originalOptions, bool isInCombat)
	{
		IEnumerable<CardModel> source = originalOptions;
		if (original.Rarity is not CardRarity.Status and not CardRarity.Curse)
		{
			source = source.Where(static card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare);
		}

		if (isInCombat)
		{
			source = source.Where(static card => card.CanBeGeneratedInCombat);
		}

		source = source.Where(card => card.Id != original.Id);
		CardModel[] options = FilterForPlayerCount(original.Owner!.RunState, source)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToArray();
		if (options.Length == 0)
		{
			throw new InvalidOperationException("All transformation options provided are invalid! Original options: " + string.Join(",", originalOptions));
		}

		return options;
	}

	private static IEnumerable<CardModel> FilterForPlayerCount(IRunState runState, IEnumerable<CardModel> options)
	{
		return runState.Players.Count > 1
			? options.Where(static card => card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
			: options;
	}
}

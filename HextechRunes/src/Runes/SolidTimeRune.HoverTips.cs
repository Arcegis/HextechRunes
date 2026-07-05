using System.Text;
using MegaCrit.Sts2.Core.Localization;

namespace HextechRunes;

public sealed partial class SolidTimeRune
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			List<StoredCard> cards = DecodeStoredCards();
			if (cards.Count <= 0)
			{
				yield break;
			}

			yield return new HoverTip(
				new LocString("relics", "solidTimeRune.storedCards.title"),
				BuildStoredCardsDescription(cards))
			{
				ShouldOverrideTextOverflow = true
			};
		}
	}

	private static string BuildStoredCardsDescription(IReadOnlyList<StoredCard> cards)
	{
		StringBuilder builder = new();
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel? preview = CreatePreviewCard(cards[i]);
			if (preview == null)
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.AppendLine();
			}

			builder.Append("- ");
			builder.Append(preview.Title);
		}

		return builder.Length > 0 ? builder.ToString() : "- ?";
	}
}

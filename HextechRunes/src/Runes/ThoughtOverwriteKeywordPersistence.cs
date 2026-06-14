using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class ThoughtOverwriteKeywordPersistence
{
	private static readonly ConditionalWeakTable<CardModel, Marker> TrackedCards = new();

	private sealed class Marker
	{
	}

	public static void Track(CardModel? card)
	{
		if (card == null)
		{
			return;
		}

		TrackedCards.GetValue(card, static _ => new Marker());
	}

	public static bool IsTracked(CardModel? card)
	{
		return card != null && TrackedCards.TryGetValue(card, out _);
	}

	public static void Restore(CardModel card)
	{
		Track(card);
		if (!card.Keywords.Contains(CardKeyword.Ethereal))
		{
			card.AddKeyword(CardKeyword.Ethereal);
		}
	}

	public static bool ShouldPersist(CardModel card)
	{
		return IsTracked(card) || IsTracked(card.DeckVersion);
	}
}

internal static class CurtainCallKeywordPersistence
{
	private static readonly ConditionalWeakTable<CardModel, Marker> TrackedCards = new();

	private sealed class Marker
	{
	}

	public static void Track(CardModel? card)
	{
		if (card == null)
		{
			return;
		}

		TrackedCards.GetValue(card, static _ => new Marker());
	}

	public static bool IsTracked(CardModel? card)
	{
		return card != null && TrackedCards.TryGetValue(card, out _);
	}

	public static void Restore(CardModel card)
	{
		Track(card);
		if (!card.Keywords.Contains(CardKeyword.Retain))
		{
			card.AddKeyword(CardKeyword.Retain);
		}
	}

	public static bool ShouldPersist(CardModel card)
	{
		return IsTracked(card) || IsTracked(card.DeckVersion);
	}
}

internal static class CosplayInnateKeywordPersistence
{
	private static readonly ConditionalWeakTable<CardModel, Marker> TrackedCards = new();

	private sealed class Marker
	{
	}

	public static void Track(CardModel? card)
	{
		if (card == null)
		{
			return;
		}

		TrackedCards.GetValue(card, static _ => new Marker());
	}

	public static bool IsTracked(CardModel? card)
	{
		return card != null && TrackedCards.TryGetValue(card, out _);
	}

	public static void Restore(CardModel card)
	{
		Track(card);
		if (!card.Keywords.Contains(CardKeyword.Innate))
		{
			card.AddKeyword(CardKeyword.Innate);
		}
	}

	public static bool ShouldPersist(CardModel card)
	{
		return IsTracked(card) || IsTracked(card.DeckVersion);
	}
}

internal static class CorruptedBranchInnateKeywordPersistence
{
	private static readonly ConditionalWeakTable<CardModel, Marker> TrackedCards = new();

	private sealed class Marker
	{
	}

	public static void Track(CardModel? card)
	{
		if (card == null)
		{
			return;
		}

		TrackedCards.GetValue(card, static _ => new Marker());
	}

	public static bool IsTracked(CardModel? card)
	{
		return card != null && TrackedCards.TryGetValue(card, out _);
	}

	public static void Restore(CardModel card)
	{
		Track(card);
		if (!card.Keywords.Contains(CardKeyword.Innate))
		{
			card.AddKeyword(CardKeyword.Innate);
		}
	}

	public static bool ShouldPersist(CardModel card)
	{
		return IsTracked(card) || IsTracked(card.DeckVersion);
	}
}

internal static class ThoughtOverwriteKeywordPersistenceHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.ToSerializable), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(ThoughtOverwriteKeywordPersistenceHooks), nameof(CardToSerializablePostfix)));
		harmony.Patch(
			RequireMethod(typeof(CardModel), nameof(CardModel.FromSerializable), BindingFlags.Static | BindingFlags.Public, typeof(SerializableCard)),
			postfix: new HarmonyMethod(typeof(ThoughtOverwriteKeywordPersistenceHooks), nameof(CardFromSerializablePostfix)));
	}

	private static void CardToSerializablePostfix(CardModel __instance, SerializableCard __result)
	{
		if (ThoughtOverwriteKeywordPersistence.ShouldPersist(__instance))
		{
			AddMarker(__result, ThoughtOverwriteRune.EtherealMarkerSavedPropertyName);
		}

		if (CurtainCallKeywordPersistence.ShouldPersist(__instance))
		{
			AddMarker(__result, CurtainCallRune.RetainMarkerSavedPropertyName);
		}

		if (CosplayInnateKeywordPersistence.ShouldPersist(__instance))
		{
			AddMarker(__result, CosplayRune.InnateMarkerSavedPropertyName);
		}

		if (CorruptedBranchInnateKeywordPersistence.ShouldPersist(__instance))
		{
			AddMarker(__result, CorruptedBranchRune.InnateMarkerSavedPropertyName);
		}
	}

	private static void CardFromSerializablePostfix(SerializableCard save, CardModel __result)
	{
		if (HasMarker(save.Props, ThoughtOverwriteRune.EtherealMarkerSavedPropertyName))
		{
			ThoughtOverwriteKeywordPersistence.Restore(__result);
		}

		if (HasMarker(save.Props, CurtainCallRune.RetainMarkerSavedPropertyName))
		{
			CurtainCallKeywordPersistence.Restore(__result);
		}

		if (HasMarker(save.Props, CosplayRune.InnateMarkerSavedPropertyName))
		{
			CosplayInnateKeywordPersistence.Restore(__result);
		}

		if (HasMarker(save.Props, CorruptedBranchRune.InnateMarkerSavedPropertyName))
		{
			CorruptedBranchInnateKeywordPersistence.Restore(__result);
		}
	}

	private static void AddMarker(SerializableCard card, string markerSavedPropertyName)
	{
		card.Props ??= new SavedProperties();
		card.Props.ints ??= new List<SavedProperties.SavedProperty<int>>();
		if (card.Props.ints.Any(property => property.name == markerSavedPropertyName))
		{
			return;
		}

		card.Props.ints.Add(new SavedProperties.SavedProperty<int>(
			markerSavedPropertyName,
			1));
	}

	private static bool HasMarker(SavedProperties? props, string markerSavedPropertyName)
	{
		return props?.ints?.Any(property =>
			property.name == markerSavedPropertyName
			&& property.value != 0) == true;
	}
}

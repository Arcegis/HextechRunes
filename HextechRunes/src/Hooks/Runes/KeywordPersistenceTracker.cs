using System.Runtime.CompilerServices;

namespace HextechRunes;

/// <summary>
/// 一种"运行期附加、需要跨克隆/升级/存档保留"的关键词：追踪哪些卡牌实例带着它，并对应一个存档标记名。
/// 追踪按实例（弱引用），战斗副本通过 <see cref="CardModel.DeckVersion"/> 继承牌组本体的追踪。
/// </summary>
internal sealed class KeywordPersistenceTracker
{
	private static readonly object TrackedMarker = new();

	private readonly ConditionalWeakTable<CardModel, object> _trackedCards = new();

	internal KeywordPersistenceTracker(CardKeyword keyword, string markerSavedPropertyName)
	{
		Keyword = keyword;
		MarkerSavedPropertyName = markerSavedPropertyName;
	}

	internal CardKeyword Keyword { get; }

	/// <summary>写进 <see cref="SerializableCard.Props"/> 的整数标记名（值 1），是存档兼容契约。</summary>
	internal string MarkerSavedPropertyName { get; }

	internal void Track(CardModel? card)
	{
		if (card == null)
		{
			return;
		}

		_trackedCards.GetValue(card, static _ => TrackedMarker);
	}

	internal bool IsTracked(CardModel? card)
	{
		return card != null && _trackedCards.TryGetValue(card, out _);
	}

	internal void Restore(CardModel card)
	{
		Track(card);
		if (!card.Keywords.Contains(Keyword))
		{
			card.AddKeyword(Keyword);
		}
	}

	internal bool ShouldPersist(CardModel card)
	{
		return IsTracked(card) || IsTracked(card.DeckVersion);
	}
}

// 以下五个入口供各符文调用，名称保持不变；实际状态都在各自的 KeywordPersistenceTracker 里。

internal static class ThoughtOverwriteKeywordPersistence
{
	internal static readonly KeywordPersistenceTracker Tracker = new(CardKeyword.Ethereal, ThoughtOverwriteRune.EtherealMarkerSavedPropertyName);

	internal static void Track(CardModel? card) => Tracker.Track(card);

	internal static bool IsTracked(CardModel? card) => Tracker.IsTracked(card);

	internal static void Restore(CardModel card) => Tracker.Restore(card);
}

internal static class CurtainCallKeywordPersistence
{
	internal static readonly KeywordPersistenceTracker Tracker = new(CardKeyword.Retain, CurtainCallRune.RetainMarkerSavedPropertyName);

	internal static void Track(CardModel? card) => Tracker.Track(card);

	internal static bool IsTracked(CardModel? card) => Tracker.IsTracked(card);

	internal static void Restore(CardModel card) => Tracker.Restore(card);
}

internal static class CosplayInnateKeywordPersistence
{
	internal static readonly KeywordPersistenceTracker Tracker = new(CardKeyword.Innate, HextechRunesApi.PersistentInnateMarkerSavedPropertyName);

	internal static void Track(CardModel? card) => Tracker.Track(card);

	internal static bool IsTracked(CardModel? card) => Tracker.IsTracked(card);

	internal static void Restore(CardModel card) => Tracker.Restore(card);
}

internal static class CorruptedBranchInnateKeywordPersistence
{
	internal static readonly KeywordPersistenceTracker Tracker = new(CardKeyword.Innate, CorruptedBranchRune.InnateMarkerSavedPropertyName);

	internal static void Track(CardModel? card) => Tracker.Track(card);

	internal static bool IsTracked(CardModel? card) => Tracker.IsTracked(card);

	internal static void Restore(CardModel card) => Tracker.Restore(card);
}

internal static class UndyingEtherealKeywordPersistence
{
	internal static readonly KeywordPersistenceTracker Tracker = new(CardKeyword.Ethereal, UndyingUpgradeRune.EtherealMarkerSavedPropertyName);

	internal static void Track(CardModel? card) => Tracker.Track(card);

	internal static bool IsTracked(CardModel? card) => Tracker.IsTracked(card);

	internal static void Restore(CardModel card) => Tracker.Restore(card);
}

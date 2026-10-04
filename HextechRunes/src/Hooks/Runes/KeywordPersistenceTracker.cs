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

	/// <summary>战斗副本从牌组本体继承追踪：本体被追踪时把关键词补回副本。</summary>
	internal void RestoreFromDeckVersion(CardModel card)
	{
		if (IsTracked(card.DeckVersion))
		{
			Restore(card);
		}
	}
}

/// <summary>各符文的关键词追踪器；标记名是存档契约，不能改。</summary>
internal static class KeywordPersistenceTrackers
{
	internal static readonly KeywordPersistenceTracker ThoughtOverwrite = new(CardKeyword.Ethereal, ThoughtOverwriteRune.EtherealMarkerSavedPropertyName);

	internal static readonly KeywordPersistenceTracker CurtainCall = new(CardKeyword.Retain, CurtainCallRune.RetainMarkerSavedPropertyName);

	internal static readonly KeywordPersistenceTracker CosplayInnate = new(CardKeyword.Innate, HextechRunesApi.PersistentInnateMarkerSavedPropertyName);

	internal static readonly KeywordPersistenceTracker CorruptedBranchInnate = new(CardKeyword.Innate, CorruptedBranchRune.InnateMarkerSavedPropertyName);

	internal static readonly KeywordPersistenceTracker UndyingEthereal = new(CardKeyword.Ethereal, UndyingUpgradeRune.EtherealMarkerSavedPropertyName);
}

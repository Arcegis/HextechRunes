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

internal static class HextechThoughtOverwriteKeywordPersistenceHooks
{
	// 顺序即存档里标记项的追加顺序与读档恢复顺序，保持与旧实现一致。
	private static readonly KeywordPersistenceTracker[] Trackers =
	[
		ThoughtOverwriteKeywordPersistence.Tracker,
		CurtainCallKeywordPersistence.Tracker,
		CosplayInnateKeywordPersistence.Tracker,
		CorruptedBranchInnateKeywordPersistence.Tracker,
		UndyingEtherealKeywordPersistence.Tracker
	];

	/// <summary>按 <see cref="Trackers"/> 下标记录的"需要保留"位图。</summary>
	private readonly struct KeywordPersistenceSnapshot
	{
		private readonly int _persistMask;

		private KeywordPersistenceSnapshot(int persistMask)
		{
			_persistMask = persistMask;
		}

		public static KeywordPersistenceSnapshot Capture(CardModel? card)
		{
			if (card == null)
			{
				return default;
			}

			int mask = 0;
			for (int i = 0; i < Trackers.Length; i++)
			{
				if (Trackers[i].ShouldPersist(card))
				{
					mask |= 1 << i;
				}
			}

			return new KeywordPersistenceSnapshot(mask);
		}

		public void Restore(CardModel? card)
		{
			if (card == null)
			{
				return;
			}

			for (int i = 0; i < Trackers.Length; i++)
			{
				if ((_persistMask & (1 << i)) != 0)
				{
					Trackers[i].Restore(card);
				}
			}
		}
	}

	/// <summary>存档：按固定顺序为需要保留的关键词追加值为 1 的标记（已有同名项不重复写）。</summary>
	internal static void WriteMarkers(CardModel card, SerializableCard save)
	{
		foreach (KeywordPersistenceTracker tracker in Trackers)
		{
			if (tracker.ShouldPersist(card))
			{
				HextechCardSavedProps.AddIntIfMissing(save, tracker.MarkerSavedPropertyName, 1);
			}
		}
	}

	/// <summary>读档：带非 0 标记的关键词恢复到卡上并重新追踪。</summary>
	internal static void RestoreFromMarkers(SerializableCard save, CardModel card)
	{
		foreach (KeywordPersistenceTracker tracker in Trackers)
		{
			if (HextechCardSavedProps.HasNonZeroInt(save.Props, tracker.MarkerSavedPropertyName))
			{
				tracker.Restore(card);
			}
		}
	}

	// 原版克隆(DeepCloneFields)只按"有来源"的关键词重建 _keywords,思维覆写/谢幕/扮演/腐化枝/不死这类
	// 运行期附加的关键词与追踪标记都会丢;镜中倒影、复视等复制整副牌组的路径拿到的副本因此没有虚无词条
	// (玩家反馈)。牌组级克隆把源牌的持久化快照原样恢复到副本上,副本自己成为被追踪的牌组版本。
	[HarmonyPatch(typeof(RunState), nameof(RunState.CloneCard), typeof(CardModel))]
	[HextechPatch("card.keyword-persistence.clone-deck", "关键词持久化")]
	private static class RunStateCloneCardPatch
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel mutableCard, CardModel __result)
		{
			KeywordPersistenceSnapshot.Capture(mutableCard).Restore(__result);
		}
	}

	[HarmonyPatch(typeof(CombatState), nameof(CombatState.CloneCard), typeof(CardModel))]
	[HextechPatch("card.keyword-persistence.clone-combat", "关键词持久化")]
	private static class CombatStateCloneCardPatch
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel mutableCard, CardModel __result)
		{
			KeywordPersistenceSnapshot.Capture(mutableCard).Restore(__result);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.ToSerializable), new Type[0])]
	[HextechPatch("card.keyword-persistence.save", "关键词持久化")]
	private static class ToSerializablePatch
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, SerializableCard __result)
		{
			WriteMarkers(__instance, __result);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable), typeof(SerializableCard))]
	[HextechPatch("card.keyword-persistence.load", "关键词持久化")]
	private static class FromSerializablePatch
	{
		[HarmonyPostfix]
		private static void Postfix(SerializableCard save, CardModel __result)
		{
			RestoreFromMarkers(save, __result);
		}
	}

	[HarmonyPatch]
	[HextechPatch("card.keyword-persistence.rebuild", "关键词持久化")]
	private static class KeywordRebuildPatch
	{
		[HarmonyTargetMethods]
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.DowngradeInternal), Type.EmptyTypes);
			yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal), Type.EmptyTypes);
		}

		[HarmonyPrefix]
		private static void Prefix(CardModel __instance, out KeywordPersistenceSnapshot __state)
		{
			__state = KeywordPersistenceSnapshot.Capture(__instance);
		}

		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, KeywordPersistenceSnapshot __state)
		{
			__state.Restore(__instance);
		}
	}
}

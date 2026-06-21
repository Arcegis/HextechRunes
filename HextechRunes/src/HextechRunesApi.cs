using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public static class HextechRunesApi
{
	public const string PersistentInnateMarkerSavedPropertyName = "SavedCosplayInnateMarker";

	public static void RegisterPlayerRune<TRune>(
		HextechRarityTier rarity,
		PlayerRuneFlags flags = PlayerRuneFlags.None,
		PlayerRuneCharacterPool? characterPool = null,
		int characterOrder = 0,
		string tagKey = "COMPREHENSIVE",
		string? assetModId = null)
		where TRune : HextechRelicBase
	{
		RegisterPlayerRune(typeof(TRune), rarity, flags, characterPool, characterOrder, tagKey, assetModId);
	}

	public static void RegisterPlayerRune(
		Type runeType,
		HextechRarityTier rarity,
		PlayerRuneFlags flags = PlayerRuneFlags.None,
		PlayerRuneCharacterPool? characterPool = null,
		int characterOrder = 0,
		string tagKey = "COMPREHENSIVE",
		string? assetModId = null)
	{
		if (runeType.IsAbstract || !typeof(HextechRelicBase).IsAssignableFrom(runeType))
		{
			throw new ArgumentException($"Player rune type must be a concrete {nameof(HextechRelicBase)}: {runeType.FullName}", nameof(runeType));
		}

		PlayerRuneRegistration registration = new(runeType, rarity, flags, characterPool, characterOrder, tagKey);
		HextechExternalContentRegistry.RegisterPlayerRune(registration, assetModId);
		HextechSavedPropertyBootstrap.InjectModelType(runeType);
		HextechModelPoolRegistrar.RegisterPlayerRuneModels([ runeType ]);
	}

	public static void RegisterEventRelic<TRelic>(string? assetModId = null)
		where TRelic : RelicModel
	{
		RegisterEventRelic(typeof(TRelic), assetModId);
	}

	public static void RegisterEventRelic(Type relicType, string? assetModId = null)
	{
		if (relicType.IsAbstract || !typeof(RelicModel).IsAssignableFrom(relicType))
		{
			throw new ArgumentException($"Event relic type must be a concrete {nameof(RelicModel)}: {relicType.FullName}", nameof(relicType));
		}

		HextechExternalContentRegistry.RegisterEventRelic(relicType, assetModId);
		HextechSavedPropertyBootstrap.InjectModelType(relicType);
		HextechModelPoolRegistrar.RegisterEventRelicModels([ relicType ]);
	}

	public static void TrackPersistentInnate(CardModel? card)
	{
		CosplayInnateKeywordPersistence.Track(card);
	}

	public static bool IsPersistentInnateTracked(CardModel? card)
	{
		return CosplayInnateKeywordPersistence.IsTracked(card);
	}

	public static void RestorePersistentInnate(CardModel card)
	{
		CosplayInnateKeywordPersistence.Restore(card);
	}
}

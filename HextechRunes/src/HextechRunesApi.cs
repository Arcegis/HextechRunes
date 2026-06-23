using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Entities.Players;

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

	public static void RegisterForge<TForge>(HextechRarityTier rarity, string? assetModId = null)
		where TForge : HextechForgeBase
	{
		RegisterForge(typeof(TForge), rarity, assetModId);
	}

	public static void RegisterForge(Type forgeType, HextechRarityTier rarity, string? assetModId = null)
	{
		if (forgeType.IsAbstract || !typeof(HextechForgeBase).IsAssignableFrom(forgeType))
		{
			throw new ArgumentException($"Forge type must be a concrete {nameof(HextechForgeBase)}: {forgeType.FullName}", nameof(forgeType));
		}

		HextechExternalContentRegistry.RegisterForge(new ForgeRegistration(forgeType, rarity), assetModId);
		HextechSavedPropertyBootstrap.InjectModelType(forgeType);
		HextechModelPoolRegistrar.RegisterForgeModels([ forgeType ]);
	}

	public static Task ObtainRandomForges(
		Player player,
		HextechRarityTier rarity,
		int count,
		Func<Type, bool> forgeTypePredicate,
		string source)
	{
		ArgumentNullException.ThrowIfNull(player);
		ArgumentNullException.ThrowIfNull(forgeTypePredicate);
		if (string.IsNullOrWhiteSpace(source))
		{
			throw new ArgumentException("Random forge source must not be empty.", nameof(source));
		}

		return HextechForgeGrantHelper.ObtainRandomForges(player, rarity, count, forgeTypePredicate, source);
	}

	public static Task<RelicModel?> SelectRelicOption(
		Player player,
		IReadOnlyList<RelicModel> options,
		string context,
		bool syncMultiplayerChoice = true)
	{
		ArgumentNullException.ThrowIfNull(player);
		ArgumentNullException.ThrowIfNull(options);
		if (string.IsNullOrWhiteSpace(context))
		{
			throw new ArgumentException("Relic option selection context must not be empty.", nameof(context));
		}

		return HextechRelicOptionSelectionCoordinator.SelectRelicOption(player, options, context, syncMultiplayerChoice);
	}

	public static void RegisterEnchantmentIcon<TEnchantment>(string iconPath)
		where TEnchantment : EnchantmentModel
	{
		RegisterEnchantmentIcon(typeof(TEnchantment), iconPath);
	}

	public static void RegisterEnchantmentIcon(Type enchantmentType, string iconPath)
	{
		if (enchantmentType.IsAbstract || !typeof(EnchantmentModel).IsAssignableFrom(enchantmentType))
		{
			throw new ArgumentException($"Enchantment type must be a concrete {nameof(EnchantmentModel)}: {enchantmentType.FullName}", nameof(enchantmentType));
		}
		if (string.IsNullOrWhiteSpace(iconPath))
		{
			throw new ArgumentException("Enchantment icon path must not be empty.", nameof(iconPath));
		}

		HextechExternalContentRegistry.RegisterEnchantmentIcon(enchantmentType, iconPath);
		HextechSavedPropertyBootstrap.InjectModelType(enchantmentType);
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

namespace HextechRunes;

[Flags]
internal enum PlayerRuneFlags
{
	None = 0,
	Disabled = 1,
	AttributeConversionExclusive = 2,
	FirstActExcluded = 4,
	ThirdActExcluded = 8,
	SelectionExcluded = 16
}

internal enum PlayerRuneCharacterPool
{
	Ironclad,
	Silent,
	Regent,
	Defect,
	Necrobinder
}

internal readonly record struct PlayerRuneRegistration(
	Type Type,
	HextechRarityTier Rarity,
	PlayerRuneFlags Flags = PlayerRuneFlags.None,
	PlayerRuneCharacterPool? CharacterPool = null,
	int CharacterOrder = 0,
	string TagKey = HextechPlayerRuneRegistry.DefaultTagKey);

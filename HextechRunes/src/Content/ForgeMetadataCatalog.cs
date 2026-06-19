namespace HextechRunes;

internal sealed class ForgeMetadataCatalog
{
	private readonly IReadOnlyList<ForgeRegistration> _registrations;
	private readonly IReadOnlyDictionary<Type, ForgeRegistration> _registrationByType;

	public ForgeMetadataCatalog(IReadOnlyList<ForgeRegistration> registrations)
	{
		_registrations = registrations.ToArray();
		_registrationByType = _registrations.ToDictionary(static registration => registration.Type);
		TypesByRarity = Enum.GetValues<HextechRarityTier>()
			.ToDictionary(static rarity => rarity, GetTypesForRarity);
		AllTypes = Enum.GetValues<HextechRarityTier>()
			.SelectMany(rarity => TypesByRarity[rarity])
			.Distinct()
			.ToArray();
	}

	public IReadOnlyList<ForgeRegistration> Registrations => _registrations;

	public IReadOnlyList<Type> AllTypes { get; }

	public IReadOnlyDictionary<HextechRarityTier, IReadOnlyList<Type>> TypesByRarity { get; }

	public bool IsRegistered(Type forgeType)
	{
		return _registrationByType.ContainsKey(forgeType);
	}

	public bool TryGetRarity(Type forgeType, out HextechRarityTier rarity)
	{
		if (_registrationByType.TryGetValue(forgeType, out ForgeRegistration registration))
		{
			rarity = registration.Rarity;
			return true;
		}

		rarity = default;
		return false;
	}

	public IReadOnlyList<Type> GetTypesForRarity(HextechRarityTier rarity)
	{
		return _registrations
			.Where(registration => registration.Rarity == rarity)
			.Select(static registration => registration.Type)
			.ToArray();
	}
}

namespace HextechRunes;

internal sealed class MonsterHexMetadataCatalog
{
	private readonly IReadOnlyList<MonsterHexRegistration> _registrations;
	private readonly IReadOnlyDictionary<MonsterHexKind, MonsterHexRegistration> _registrationByKind;

	public MonsterHexMetadataCatalog(IReadOnlyList<MonsterHexRegistration> registrations)
	{
		_registrations = registrations.ToArray();
		_registrationByKind = _registrations.ToDictionary(static registration => registration.Kind);
		EnabledKindsByRarity = Enum.GetValues<HextechRarityTier>()
			.ToDictionary(static rarity => rarity, GetEnabledKindsForRarity);
		DisabledKinds = _registrations
			.Where(static registration => registration.Disabled)
			.Select(static registration => registration.Kind)
			.ToHashSet();
		BurnHoverTipKinds = _registrations
			.Where(static registration => registration.HasBurnHoverTip)
			.Select(static registration => registration.Kind)
			.ToHashSet();
		IconRelicTypes = _registrations
			.ToDictionary(static registration => registration.Kind, static registration => registration.IconRelicType);
		AllKinds = _registrations
			.Select(static registration => registration.Kind)
			.ToHashSet();
	}

	public IReadOnlyList<MonsterHexRegistration> Registrations => _registrations;

	public IReadOnlyDictionary<HextechRarityTier, IReadOnlyList<MonsterHexKind>> EnabledKindsByRarity { get; }

	public IReadOnlySet<MonsterHexKind> DisabledKinds { get; }

	public IReadOnlySet<MonsterHexKind> BurnHoverTipKinds { get; }

	public IReadOnlyDictionary<MonsterHexKind, Type> IconRelicTypes { get; }

	public IReadOnlySet<MonsterHexKind> AllKinds { get; }

	public bool IsRegistered(MonsterHexKind kind)
	{
		return _registrationByKind.ContainsKey(kind);
	}

	public bool TryGetRegistration(MonsterHexKind kind, out MonsterHexRegistration registration)
	{
		return _registrationByKind.TryGetValue(kind, out registration);
	}

	public bool IsEnabled(MonsterHexKind kind)
	{
		return _registrationByKind.TryGetValue(kind, out MonsterHexRegistration registration)
			&& !registration.Disabled;
	}

	public IReadOnlyList<MonsterHexKind> GetEnabledKindsForRarity(HextechRarityTier rarity)
	{
		return _registrations
			.Where(registration => registration.Rarity == rarity && !registration.Disabled)
			.Select(static registration => registration.Kind)
			.ToArray();
	}
}

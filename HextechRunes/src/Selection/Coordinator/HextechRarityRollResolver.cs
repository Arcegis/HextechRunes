namespace HextechRunes;

internal readonly record struct HextechRarityWeights(int Silver, int Gold, int Prismatic)
{
	public int Total => Silver + Gold + Prismatic;
}

internal static class HextechRarityRollResolver
{
	public static HextechRarityWeights ApplyEnabledRarities(
		int silverWeight,
		int goldWeight,
		int prismaticWeight,
		IReadOnlyCollection<HextechRarityTier> enabledRarities)
	{
		return new HextechRarityWeights(
			enabledRarities.Contains(HextechRarityTier.Silver) ? silverWeight : 0,
			enabledRarities.Contains(HextechRarityTier.Gold) ? goldWeight : 0,
			enabledRarities.Contains(HextechRarityTier.Prismatic) ? prismaticWeight : 0);
	}

	public static HextechRarityTier ResolveWeighted(HextechRarityWeights weights, int roll)
	{
		if (weights.Total <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(weights), weights, "Cannot resolve a weighted rarity with no positive weight.");
		}

		if (roll < weights.Silver)
		{
			return HextechRarityTier.Silver;
		}

		roll -= weights.Silver;
		if (roll < weights.Gold)
		{
			return HextechRarityTier.Gold;
		}

		return HextechRarityTier.Prismatic;
	}

	public static HextechRarityTier[] GetUniformRarityOrder(IReadOnlyCollection<HextechRarityTier> enabledRarities)
	{
		HextechRarityTier[] orderedRarities = enabledRarities
			.OrderBy(static rarity => (int)rarity)
			.ToArray();
		return orderedRarities.Length == 0
			? Enum.GetValues<HextechRarityTier>()
			: orderedRarities;
	}

	public static HextechRarityTier ResolveUniform(IReadOnlyCollection<HextechRarityTier> enabledRarities, int roll)
	{
		HextechRarityTier[] orderedRarities = GetUniformRarityOrder(enabledRarities);
		return orderedRarities[roll];
	}

	public static bool HasAllRarities(IReadOnlyCollection<HextechRarityTier> enabledRarities)
	{
		return Enum.GetValues<HextechRarityTier>()
			.All(enabledRarities.Contains);
	}
}

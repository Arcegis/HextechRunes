namespace HextechRunes;

internal sealed record HextechRunConfigurationSnapshot(
	int[] PlayerHexCountsByAct,
	int[] EnemyHexCountsByAct,
	int PlayerRuneRerollLimit,
	int MonsterHexRerollLimit,
	HashSet<string> DisabledPlayerRuneIds,
	HashSet<string> DisabledMonsterHexIds,
	HashSet<string> DisabledForgeIds,
	HextechRarityWeights FirstActRuneRarityWeights,
	HextechRarityWeights NormalRuneRarityWeights,
	HextechRarityWeights SecondActAfterSilverRuneRarityWeights,
	HextechForgeRarityWeights ForgeRarityWeights,
	int RandomForgeShopPrice,
	bool RandomForgeDirectGrant)
{
	public HextechRunConfigurationSnapshot Copy()
	{
		return this with
		{
			PlayerHexCountsByAct = PlayerHexCountsByAct.ToArray(),
			EnemyHexCountsByAct = EnemyHexCountsByAct.ToArray(),
			DisabledPlayerRuneIds = DisabledPlayerRuneIds.ToHashSet(StringComparer.Ordinal),
			DisabledMonsterHexIds = DisabledMonsterHexIds.ToHashSet(StringComparer.Ordinal),
			DisabledForgeIds = DisabledForgeIds.ToHashSet(StringComparer.Ordinal)
		};
	}
}

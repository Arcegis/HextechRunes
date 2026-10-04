namespace HextechRunes;

public sealed class TransmuteGoldRune : TransmuteRuneBase
{
	protected override IEnumerable<Type> CandidateTypes => HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Gold);
}

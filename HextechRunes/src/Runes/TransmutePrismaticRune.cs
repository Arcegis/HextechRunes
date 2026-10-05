namespace HextechRunes;

public sealed class TransmutePrismaticRune : TransmuteRuneBase
{
	protected override IEnumerable<Type> CandidateTypes => HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic);
}

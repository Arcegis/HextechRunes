namespace HextechRunes;

public sealed class TransmuteChaosRune : TransmuteRuneBase
{
	protected override IEnumerable<Type> CandidateTypes => HextechCatalog.GetAllConfigurableRuneTypes();

	protected override int ObtainCount => 2;
}

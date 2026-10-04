namespace HextechRunes;

/// <summary>
/// 属性系列：拾取时发放 ForgeCount 次锻造器，三档只差次数。
/// </summary>
public abstract class StatsRuneBase : InitialForgeGrantRune
{
	protected abstract int ForgeCount { get; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", ForgeCount)
	];

	protected override int InitialForgeCount => DynamicVars["ForgeCount"].IntValue;
}

public sealed class StatsRune : StatsRuneBase
{
	protected override int ForgeCount => 2;
}

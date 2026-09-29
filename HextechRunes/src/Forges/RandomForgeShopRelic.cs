namespace HextechRunes;

public sealed class RandomForgeShopRelic : HextechRelicBase
{
	private const string PriceVarName = "Price";

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPurchaseCount
	{
		get => PurchaseCount;
		set => PurchaseCount = Math.Max(0, value);
	}

	public int PurchaseCount { get; private set; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(PriceVarName, HextechRuneConfiguration.GetDefaultRandomForgeShopPrice())
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return false;
	}

	public void SetDisplayedPrice(int price)
	{
		DynamicVars[PriceVarName].BaseValue = HextechRuneConfiguration.ClampRandomForgeShopPrice(price);
	}

	public void IncrementPurchaseCount()
	{
		PurchaseCount++;
	}
}

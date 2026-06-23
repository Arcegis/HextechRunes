using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechForgeShopPriceHelper
{
	public static int GetCurrentRandomForgeShopPrice()
	{
		try
		{
			if (RunManager.Instance.DebugOnlyGetState() is RunState runState
				&& TryGetRandomForgeShopPrice(runState, out int price))
			{
				return price;
			}
		}
		catch
		{
			// Fall back to persisted local configuration outside an initialized run.
		}

		return HextechRuneConfiguration.GetSnapshot().RandomForgeShopPrice;
	}

	public static int GetRandomForgeShopPriceFor(RunState? runState)
	{
		return TryGetRandomForgeShopPrice(runState, out int price)
			? price
			: GetCurrentRandomForgeShopPrice();
	}

	public static void RefreshRandomForgeShopRelic(RandomForgeShopRelic shopRelic, RunState? runState = null)
	{
		shopRelic.SetDisplayedPrice(GetRandomForgeShopPriceFor(runState));
	}

	private static bool TryGetRandomForgeShopPrice(RunState? runState, out int price)
	{
		price = 0;
		HextechMayhemModifier? modifier = runState?.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault();
		if (modifier == null)
		{
			return false;
		}

		price = modifier.RandomForgeShopPrice;
		return true;
	}
}

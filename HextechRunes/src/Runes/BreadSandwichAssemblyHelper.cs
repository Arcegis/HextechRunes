using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class BreadSandwichAssemblyHelper
{
	public static async Task TryAssemble(Player player)
	{
		if (player.GetRelic<BreadSandwichRune>() != null)
		{
			return;
		}

		BreadAndButterRune? butter = player.GetRelic<BreadAndButterRune>();
		BreadAndCheeseRune? cheese = player.GetRelic<BreadAndCheeseRune>();
		BreadAndJamRune? jam = player.GetRelic<BreadAndJamRune>();
		if (butter == null || cheese == null || jam == null)
		{
			return;
		}

		foreach (RelicModel rune in new RelicModel[] { butter, cheese, jam })
		{
			await RelicCmd.Remove(rune);
		}

		RelicModel sandwich = ModelDb.GetById<RelicModel>(ModelDb.GetId<BreadSandwichRune>()).ToMutable();
		SaveManager.Instance.MarkRelicAsSeen(sandwich);
		await RelicCmd.Obtain(sandwich, player);
	}
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class BreadSandwichAssemblyHelper
{
	public static async Task TryAssemble(Player? player)
	{
		if (player == null || player.GetRelic<BreadSandwichRune>() != null)
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

		RelicModel[] consumedRunes = [butter, cheese, jam];
		foreach (RelicModel rune in consumedRunes)
		{
			if (player.Relics.Contains(rune))
			{
				await RelicCmd.Remove(rune);
			}
		}

		RelicModel sandwich = ModelDb.GetById<RelicModel>(ModelDb.GetId<BreadSandwichRune>()).ToMutable();
		SaveManager.Instance.MarkRelicAsSeen(sandwich);
		await RelicCmd.Obtain(sandwich, player);
	}
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public static class RelicBundleGrantHelper
{
	public static async Task GrantRelics(Player player, IEnumerable<Type> relicTypes)
	{
		foreach (Type type in relicTypes)
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type)).ToMutable();
			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, player);
		}
	}
}

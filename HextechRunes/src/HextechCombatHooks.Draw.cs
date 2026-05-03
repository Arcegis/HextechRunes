using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static bool DrawPrefix(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
	{
		NoNonsenseRune? noNonsenseRune = player.GetRelic<NoNonsenseRune>();
		if (noNonsenseRune == null || fromHandDraw || count <= 0m || player.Creature.CombatState == null)
		{
			return true;
		}

		int drawsPrevented = (int)Math.Ceiling(count);
		if (drawsPrevented <= 0)
		{
			__result = Task.FromResult<IEnumerable<CardModel>>(Array.Empty<CardModel>());
			return false;
		}

		__result = DrawNoNonsense(noNonsenseRune, drawsPrevented, player);
		return false;
	}

	private static async Task<IEnumerable<CardModel>> DrawNoNonsense(NoNonsenseRune noNonsenseRune, int drawsPrevented, Player player)
	{
		await noNonsenseRune.HandlePreventedNonHandDraw(drawsPrevented);
		await PowerCmd.Apply<StrengthPower>(player.Creature, drawsPrevented, player.Creature, null);
		return Array.Empty<CardModel>();
	}
}

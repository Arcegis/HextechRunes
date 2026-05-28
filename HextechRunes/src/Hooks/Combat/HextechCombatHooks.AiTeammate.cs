using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static bool ForbiddenGrimoireAfterCombatEndPrefix(ForbiddenGrimoirePower __instance, CombatRoom room, ref Task __result)
	{
		if (__instance.Owner?.Player is not { } owner || !HextechAiTeammateCompat.IsAiPlayer(owner))
		{
			return true;
		}

		__result = RemoveCardsForAiForbiddenGrimoire(__instance, owner);
		return false;
	}

	private static async Task RemoveCardsForAiForbiddenGrimoire(ForbiddenGrimoirePower power, Player owner)
	{
		int amount = Math.Max(0, (int)Math.Floor((decimal)power.Amount));
		for (int i = 0; i < amount; i++)
		{
			CardSelectorPrefs prefs = new(CardSelectorPrefs.RemoveSelectionPrompt, 1)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			};
			CardModel? card = (await CardSelectCmd.FromDeckForRemoval(owner, prefs)).FirstOrDefault();
			if (card == null)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Forbidden Grimoire AI removal skipped: player={owner.NetId} no removable card.");
				return;
			}

			await CardPileCmd.RemoveFromDeck(card);
			Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Forbidden Grimoire removed AI card: player={owner.NetId} card={card.Id.Entry}");
		}
	}
}

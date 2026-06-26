using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static bool StormBeforeCardPlayedPrefix(StormPower __instance, ref Task __result)
	{
		if (ShouldUseHextechStormHandling(__instance))
		{
			__result = Task.CompletedTask;
			return false;
		}

		return true;
	}

	private static bool StormAfterCardPlayedPrefix(StormPower __instance, ref Task __result)
	{
		if (ShouldUseHextechStormHandling(__instance))
		{
			__result = Task.CompletedTask;
			return false;
		}

		return true;
	}

	private static bool EntropyAfterPlayerTurnStartPrefix(EntropyPower __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
	{
		__result = SafeEntropyAfterPlayerTurnStart(__instance, choiceContext, player);
		return false;
	}

	private static async Task SafeEntropyAfterPlayerTurnStart(EntropyPower entropyPower, PlayerChoiceContext choiceContext, Player player)
	{
		if (player != entropyPower.Owner.Player)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
			choiceContext,
			player,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, entropyPower.Amount),
			CanTransformToRandomCard,
			entropyPower);

		List<CardModel> selectedCards = selected.ToList();
		for (int i = 0; i < selectedCards.Count; i++)
		{
			CardModel card = selectedCards[i];
			if (CanTransformToRandomCard(card))
			{
				await CardTransformUpgradeHelper.TransformToStableRandom(
					card,
					(RunState)player.RunState,
					"entropy-transform-replacement",
					i,
					saltParts:
					[
						HextechStableRandom.PlayerKey(player),
						player.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
						entropyPower.Amount.ToString(),
						HextechStableRandom.CardPileKey(selectedCards)
					]);
			}
		}
	}

	private static bool CanTransformToRandomCard(CardModel card)
	{
		if (!card.IsTransformable || card.Pile?.Type != PileType.Hand)
		{
			return false;
		}

		try
		{
			return CardFactory.GetDefaultTransformationOptions(card, card.CombatState != null).Any();
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private static bool ShouldUseHextechStormHandling(StormPower stormPower)
	{
		return stormPower.Owner?.CombatState?.RunState is RunState runState
			&& GetMayhemModifier(runState) != null;
	}
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class VakuuTurnController
{
	internal const int MaxCardsPlayed = 13;

	internal static async Task<int> AutoPlayPlayableHand(Player player)
	{
		if (player.Creature.CombatState is not HextechCombatState combatState)
		{
			return 0;
		}

		PlayerChoiceContext autoChoiceContext = new BlockingPlayerChoiceContext();
		int cardsPlayed;
		using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
		{
			for (cardsPlayed = 0; cardsPlayed < MaxCardsPlayed; cardsPlayed++)
			{
				if (CombatManager.Instance.IsOverOrEnding || CombatManager.Instance.IsPlayerReadyToEndTurn(player))
				{
					break;
				}

				CardModel? card = PileType.Hand.GetPile(player).Cards.FirstOrDefault(static card => card.CanPlay());
				if (card == null)
				{
					break;
				}

				Creature? target = GetTarget(player, card, combatState);
				await card.SpendResources();
				await HextechAutoPlayHelper.AutoPlayOrMoveToResultPile(autoChoiceContext, card, target, skipXCapture: true);
			}
		}

		return cardsPlayed;
	}

	internal static void PlayLineIfCardsPlayed(Player player, int cardsPlayed)
	{
		if (cardsPlayed <= 0)
		{
			return;
		}

		LocString line = cardsPlayed >= MaxCardsPlayed
			? new LocString("relics", "WHISPERING_EARRING.warning")
			: new LocString("relics", "WHISPERING_EARRING.approval");
		TalkCmd.Play(line, player.Creature, VfxColor.Purple);
	}

	private static Creature? GetTarget(Player owner, CardModel card, HextechCombatState combatState)
	{
		return card.TargetType switch
		{
			TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
			TargetType.AnyAlly => PickStableAllyTarget(owner, card, combatState),
			TargetType.AnyPlayer => owner.Creature,
			_ => null
		};
	}

	private static Creature? PickStableAllyTarget(Player owner, CardModel card, HextechCombatState combatState)
	{
		List<Creature> candidates = combatState.Allies
			.Where(creature => creature is { IsAlive: true, IsPlayer: true } && creature != owner.Creature)
			.ToList();
		if (candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)owner.RunState,
			candidates.Count,
			"vakuu-controlled-ally-target",
			HextechStableRandom.PlayerKey(owner),
			combatState.RoundNumber.ToString(),
			HextechStableRandom.CardActionKey(card),
			string.Join(",", candidates.Select(static creature => creature.CombatId?.ToString() ?? "none")));
		return candidates[index];
	}
}

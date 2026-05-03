using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private enum EnemySingularityAIStatusKind
	{
		Burn,
		Dazed,
		Slimed,
		Wound,
		Void
	}

	private static readonly IReadOnlyList<EnemySingularityAIStatusKind> EnemySingularityAIStatusPool =
	[
		EnemySingularityAIStatusKind.Burn,
		EnemySingularityAIStatusKind.Dazed,
		EnemySingularityAIStatusKind.Slimed,
		EnemySingularityAIStatusKind.Wound,
		EnemySingularityAIStatusKind.Void
	];

	private async Task AddEnemySingularityAIStatusCards(HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		foreach (Player player in players
			.Select(static creature => creature.Player)
			.OfType<Player>()
			.OrderBy(static player => player.NetId))
		{
			int statusIndex = GetEnemySingularityAIStatusIndex(player, combatState.RoundNumber);
			CardModel card = CreateEnemySingularityAIStatusCard(combatState, player, EnemySingularityAIStatusPool[statusIndex]);

			await HextechCardGeneration.AddGeneratedCardToCombat(
				card,
				PileType.Draw,
				addedByPlayer: false,
				position: CardPilePosition.Bottom);
		}
	}

	private int GetEnemySingularityAIStatusIndex(Player player, int roundNumber)
	{
		return HextechStableRandom.PlayerCombatRoundIndex(
			RunState,
			player,
			EnemySingularityAIStatusPool.Count,
			roundNumber);
	}

	private static CardModel CreateEnemySingularityAIStatusCard(
		HextechCombatState combatState,
		Player player,
		EnemySingularityAIStatusKind kind)
	{
		return kind switch
		{
			EnemySingularityAIStatusKind.Burn => combatState.CreateCard<Burn>(player),
			EnemySingularityAIStatusKind.Dazed => combatState.CreateCard<Dazed>(player),
			EnemySingularityAIStatusKind.Slimed => combatState.CreateCard<Slimed>(player),
			EnemySingularityAIStatusKind.Wound => combatState.CreateCard<Wound>(player),
			_ => combatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(player)
		};
	}
}

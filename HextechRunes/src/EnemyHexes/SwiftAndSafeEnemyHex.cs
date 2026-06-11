namespace HextechRunes;

internal sealed class SwiftAndSafeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SwiftAndSafe;

	internal override async Task AfterCardDrawn(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner?.Creature.Side != CombatSide.Player
			|| card.Owner.Creature.CombatState?.RunState != context.RunState
			|| IsNetworkMultiplayer())
		{
			return;
		}

		Player owner = card.Owner;
		ulong playerId = owner.NetId;
		int cardsPerArtifact = context.TierValue(Kind, 15, 12, 10);
		int cardsDrawn = context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat.GetValueOrDefault(playerId, 0) + 1;
		context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat[playerId] = cardsDrawn;
		if (cardsDrawn % cardsPerArtifact != 0)
		{
			return;
		}

		HextechCombatState combatState = owner.Creature.CombatState;
		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<ArtifactPower>(enemy, 1, enemy, null);
		}
	}

	internal override Task AfterCardPlayedLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return IsNetworkMultiplayer() && cardPlay.Card.Owner?.Creature.CombatState is HextechCombatState combatState
			? ResolvePlayerDraws(context, combatState)
			: Task.CompletedTask;
	}

	internal override Task AfterPlayerTurnStartLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		return IsNetworkMultiplayer() && player.Creature.CombatState is HextechCombatState combatState
			? ResolvePlayerDraws(context, combatState)
			: Task.CompletedTask;
	}

#if !STS2_104_OR_NEWER
	internal override Task BeforePlayPhaseStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		return IsNetworkMultiplayer() && player.Creature.CombatState is HextechCombatState combatState
			? ResolvePlayerDraws(context, combatState)
			: Task.CompletedTask;
	}
#endif

	internal override Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		return side == CombatSide.Player && combatRoom != null && IsNetworkMultiplayer()
			? ResolvePlayerDraws(context, combatRoom.CombatState)
			: Task.CompletedTask;
	}

	private static async Task ResolvePlayerDraws(HextechEnemyHexContext context, HextechCombatState combatState)
	{
		if (combatState.RunState != context.RunState)
		{
			return;
		}

		int pendingArtifact = 0;
		int cardsPerArtifact = context.TierValue(MonsterHexKind.SwiftAndSafe, 15, 12, 10);
		foreach (Player player in combatState.Players.OrderBy(static player => player.NetId))
		{
			int drawnCards = CountPlayerDrawnCardsFromHistory(player);
			int previousDrawnCards = context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat.GetValueOrDefault(player.NetId, 0);
			if (drawnCards <= previousDrawnCards)
			{
				continue;
			}

			pendingArtifact += drawnCards / cardsPerArtifact - previousDrawnCards / cardsPerArtifact;
			context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat[player.NetId] = drawnCards;
		}

		if (pendingArtifact <= 0)
		{
			return;
		}

		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<ArtifactPower>(enemy, pendingArtifact, enemy, null);
		}
	}

	private static int CountPlayerDrawnCardsFromHistory(Player player)
	{
		return CombatManager.Instance.History.Entries
			.OfType<CardDrawnEntry>()
			.Count(entry => entry.Card.Owner?.NetId == player.NetId);
	}

	private static bool IsNetworkMultiplayer()
	{
		return RunManager.Instance.NetService.Type is NetGameType.Host or NetGameType.Client;
	}
}

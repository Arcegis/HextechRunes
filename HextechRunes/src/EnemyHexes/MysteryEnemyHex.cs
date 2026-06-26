using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

internal sealed class MysteryEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Mystery;

	internal override async Task AfterPlayerTurnStartLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature.IsDead
			|| player.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
			.Where(CanTransformToRandomCard)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		CardModel card = HextechStableRandom.Pick(
			candidates,
			(RunState)context.RunState,
			HextechStableRandom.CardKey,
			"enemy-mystery-transform",
			HextechStableRandom.PlayerKey(player),
			combatState.RoundNumber.ToString(),
			HextechStableRandom.CardPileKey(candidates));
		int transformOrdinal = HextechCombatProcTracker.ConsumeGlobalProcInCombat(
			context.Tracking,
			string.Join(":", nameof(MysteryEnemyHex), HextechStableRandom.PlayerKey(player)));
		await CardTransformUpgradeHelper.TransformToStableRandom(
			card,
			(RunState)context.RunState,
			"enemy-mystery-transform-replacement",
			transformOrdinal,
			CardPreviewStyle.HorizontalLayout,
			HextechStableRandom.PlayerKey(player),
			combatState.RoundNumber.ToString(),
			HextechStableRandom.CardPileKey(candidates));
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
}

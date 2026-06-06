using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (TrackPlayerAttackCardPlayedThisTurn(cardPlay)
			&& cardPlay.Card.Owner?.Creature.CombatState is HextechCombatState combatState)
		{
			RefreshPlayerAttackCostDoublingPreviews(HextechCombatCreatureHelper.GetAlivePlayerSideCreatures(combatState));
		}

		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, enemyHexContext) => effect.AfterCardPlayed(enemyHexContext, context, cardPlay));
	}

	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterShuffle(context, choiceContext, shuffler));
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner?.Creature.CombatState?.RunState == RunState && card is WhiteHoleCard whiteHole)
		{
			await whiteHole.AfterDrawn();
		}

		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterCardDrawn(context, choiceContext, card, fromHandDraw));
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterCardPlayedLate(context, choiceContext, cardPlay));

		Player? owner = cardPlay.Card.Owner;
		if (owner == null
			|| cardPlay.Card.Type != CardType.Power
			|| owner.Creature.CombatState?.RunState != RunState
			|| owner.Creature.GetPower<StormPower>() is not StormPower stormPower)
		{
			return;
		}

		int lightningCount = Math.Max(0, (int)Math.Floor((decimal)stormPower.Amount));
		for (int i = 0; i < lightningCount; i++)
		{
			OrbModel orb = ModelDb.Orb<LightningOrb>().ToMutable();
			await OrbCmd.Channel(new BlockingPlayerChoiceContext(), orb, owner);
		}
	}

	public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterPlayerTurnStartLate(context, choiceContext, player));
	}

#if STS2_104_OR_NEWER
	public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterAutoPrePlayPhaseEnteredLate(context, choiceContext, player));
	}
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.BeforePlayPhaseStart(context, choiceContext, player));
	}
#endif
}

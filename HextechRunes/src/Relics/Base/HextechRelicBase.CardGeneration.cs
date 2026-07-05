using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public abstract partial class HextechRelicBase
{
	protected async Task AddCardCopiesToDeckOrHand<TCard>(int count, Action<CardModel>? configureCard = null)
		where TCard : CardModel
	{
		if (Owner == null || count <= 0)
		{
			return;
		}

		HextechCombatState? combatState = Owner.Creature.CombatState;
		if (Owner.PlayerCombatState != null
			&& combatState != null
			&& CombatManager.Instance.IsInProgress
			&& !CombatManager.Instance.IsOverOrEnding)
		{
			List<CardModel> cards = new(count);
			for (int i = 0; i < count; i++)
			{
				CardModel card = combatState.CreateCard<TCard>(Owner);
				configureCard?.Invoke(card);
				cards.Add(card);
			}

			await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);

			return;
		}

		List<CardPileAddResult> results = new(count);
		for (int i = 0; i < count; i++)
		{
			CardModel card = Owner.RunState.CreateCard<TCard>(Owner);
			configureCard?.Invoke(card);
			results.Add(await CardPileCmd.Add(card, PileType.Deck));
			SaveManager.Instance.MarkCardAsSeen(card);
		}

		CardCmd.PreviewCardPileAdd(results, 2f);
	}

	protected async Task AddCardCopiesToCombatHand<TCard>(int count, Action<CardModel>? configureCard = null)
		where TCard : CardModel
	{
		if (Owner == null
			|| count <= 0
			|| Owner.PlayerCombatState == null
			|| Owner.Creature.CombatState is not HextechCombatState combatState
			|| !CombatManager.Instance.IsInProgress
			|| CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}

		List<CardModel> cards = new(count);
		for (int i = 0; i < count; i++)
		{
			CardModel card = combatState.CreateCard<TCard>(Owner);
			configureCard?.Invoke(card);
			cards.Add(card);
		}

		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}
}

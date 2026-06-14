using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class CorruptedBranchRune : HextechRelicBase
{
	internal const string InnateMarkerSavedPropertyName = "SavedCorruptedBranchInnateMarker";

	private int _generatedCardsThisCombat;

	public override bool HasUponPickupEffect => true;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedGeneratedCardsThisCombat
	{
		get => _generatedCardsThisCombat;
		set => _generatedCardsThisCombat = Math.Max(0, value);
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedCorruptedBranchInnateMarker
	{
		get => 0;
		set { }
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Corruption>(),
		HoverTipFactory.FromKeyword(CardKeyword.Innate)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		CardModel card = Owner.RunState.CreateCard<Corruption>(Owner);
		ApplyPersistentInnate(card);
		CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck);
		SaveManager.Instance.MarkCardAsSeen(card);
		CardCmd.PreviewCardPileAdd(result, 2f);
	}

	public override Task BeforeCombatStart()
	{
		_generatedCardsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_generatedCardsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (Owner == null || card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		if (CorruptedBranchInnateKeywordPersistence.IsTracked(card.DeckVersion))
		{
			CorruptedBranchInnateKeywordPersistence.Restore(card);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!IsOwnedCard(card)
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		CardModel? generatedCard = CreateRandomCombatCard(combatState, card);
		if (generatedCard == null)
		{
			return;
		}

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(generatedCard, PileType.Hand, addedByPlayer: true);
	}

	private CardModel? CreateRandomCombatCard(HextechCombatState combatState, CardModel sourceCard)
	{
		if (Owner == null)
		{
			return null;
		}

		List<CardModel> pool = CardFactory
			.FilterForCombat(Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint))
			.Where(static card => card.CanBeGeneratedByModifiers)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (pool.Count == 0)
		{
			return null;
		}

		int procOrdinal = ConsumeCombatProcOrdinal(nameof(CorruptedBranchRune), ref _generatedCardsThisCombat);
		CardModel canonicalCard = HextechStableRandom.Pick(
			pool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"corrupted-branch-exhaust",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			procOrdinal.ToString(),
			HextechStableRandom.CardKey(sourceCard),
			HextechStableRandom.CardPileKey(pool));

		return combatState.CreateCard(canonicalCard, Owner);
	}

	private static void ApplyPersistentInnate(CardModel card)
	{
		CorruptedBranchInnateKeywordPersistence.Track(card);
		CardCmd.ApplyKeyword(card, CardKeyword.Innate);
	}
}

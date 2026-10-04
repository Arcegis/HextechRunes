using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;

namespace HextechRunes;

public sealed class ColorDiscoveryRune : HextechRelicBase
{
	private ModelId _pendingRewardCardId = ModelId.none;
	private bool _offeredThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public ModelId SavedPendingRewardCardId
	{
		get => _pendingRewardCardId;
		set => _pendingRewardCardId = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedOfferedThisCombat
	{
		get => _offeredThisCombat;
		set => _offeredThisCombat = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3),
		new DynamicVar("Selection", 1m)
	];

	public override Task BeforeCombatStart()
	{
		SavedOfferedThisCombat = false;
		SavedPendingRewardCardId = ModelId.none;
		return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (SavedOfferedThisCombat
			|| player != Owner
			|| Owner.Creature.IsDead
			|| combatState.RoundNumber != 1
			|| PickOptions(combatState).ToList() is not { Count: > 0 } options)
		{
			return;
		}

		SavedOfferedThisCombat = true;
		IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
			choiceContext,
			options,
			Owner,
			new CardSelectorPrefs(new LocString("cards", "colorDiscoveryRune.selectionScreenPrompt"), 1));
		CardModel? card = selected.FirstOrDefault();
		if (card == null)
		{
			return;
		}

		SavedPendingRewardCardId = card.CanonicalId();
		card.SetToFreeThisCombat();

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner.Creature.IsDead || SavedPendingRewardCardId.Equals(ModelId.none))
		{
			return Task.CompletedTask;
		}

		room.AddExtraReward(Owner, new ColorDiscoveryCardReward(SavedPendingRewardCardId, Owner));
		SavedPendingRewardCardId = ModelId.none;
		Flash();
		return Task.CompletedTask;
	}

	private IEnumerable<CardModel> PickOptions(HextechCombatState combatState)
	{
		List<CardModel> candidates = GetOtherCharacterCards(Owner);
		List<CardModel> options = [];
		for (int i = 0; i < DynamicVars.Cards.IntValue && candidates.Count > 0; i++)
		{
			CardModel? card = PickStableGeneratedCard(
				combatState,
				candidates,
				out ModelId canonicalCardId,
				"color-discovery-option",
				HextechStableRandom.PlayerKey(Owner),
				combatState.RoundNumber.ToString(),
				i.ToString(),
				HextechStableRandom.CardPileKey(candidates));
			if (card == null)
			{
				break;
			}

			options.Add(card);
			candidates.RemoveAll(candidate => candidate.Id == canonicalCardId);
		}

		return options;
	}

	// 同一张卡可能出现在多个池里：按 Id 去重保留首次出现，再交给基类统一做战斗过滤、生成许可与 CardKey 稳定排序。
	private static List<CardModel> GetOtherCharacterCards(Player player)
	{
		ModelId ownerPoolId = player.Character.CardPool.Id;
		return BuildStableCombatGenerationPool(ModelDb.AllCharacters
			.Select(static character => character.CardPool)
			.Where(pool => !pool.Id.Equals(ownerPoolId))
			.SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
			.DistinctBy(static card => card.Id));
	}
}

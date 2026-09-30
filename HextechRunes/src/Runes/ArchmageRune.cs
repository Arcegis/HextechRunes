namespace HextechRunes;

public sealed class ArchmageRune : HextechRelicBase
{
	// 单机稳定随机的本地序号（见 ConsumeCombatProcOrdinal）：不在战斗开始清零，跨战斗累加、读档归零；
	// 联机改用 Mayhem 的每场计数。改成每场清零会改变单机的随机结果，按现状保留。
	private int _localFreeCardRollOrdinal;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ChancePercent", 33m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| !CombatManager.Instance.IsInProgress
			|| !IsOwnedSkill(cardPlay.Card)
			|| !RollTrigger(cardPlay.Card, out int rollOrdinal)
			|| PickCardToMakeFree(cardPlay.Card, rollOrdinal) is not CardModel card)
		{
			return Task.CompletedTask;
		}

		card.SetToFreeThisTurn();
		Flash();
		return Task.CompletedTask;
	}

	private bool RollTrigger(CardModel sourceCard, out int rollOrdinal)
	{
		rollOrdinal = -1;
		if (Owner == null)
		{
			return false;
		}

		rollOrdinal = ConsumeCombatProcOrdinal(nameof(ArchmageRune), ref _localFreeCardRollOrdinal);
		return HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["ChancePercent"].IntValue,
			"archmage-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			rollOrdinal.ToString(),
			HextechStableRandom.CardKey(sourceCard));
	}

	private CardModel? PickCardToMakeFree(CardModel sourceCard, int rollOrdinal)
	{
		if (Owner == null)
		{
			return null;
		}

		return HextechFreeCardPicker.Pick(
			PileType.Hand.GetPile(Owner).Cards,
			(candidates, tier) => PickFromCandidates(candidates, sourceCard, rollOrdinal, tier));
	}

	private CardModel? PickFromCandidates(IReadOnlyList<CardModel> candidates, CardModel sourceCard, int rollOrdinal, string tier)
	{
		if (Owner == null || candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)Owner.RunState,
			candidates.Count,
			"archmage-pick-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			rollOrdinal.ToString(),
			HextechStableRandom.CardKey(sourceCard),
			tier,
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}

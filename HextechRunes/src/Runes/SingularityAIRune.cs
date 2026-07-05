namespace HextechRunes;

public sealed class SingularityAIRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		// 只从「可在战斗中生成」的卡里取 power 卡:CardFactory.FilterForCombat 会剔除 Basic/Ancient/Event 及
		// CanBeGeneratedInCombat=false 的卡(与 DeadwoodRune/CorruptedBranchRune 同款),外加 CanBeGeneratedByModifiers。
		// 此前缺这层过滤会把 Ancient 稀有度的禁忌魔典(ForbiddenGrimoire)纳入并生成 → 联机下打出时 power 应用两端
		// 分叉(client 上了 power、host 没上)→ StateDivergence 掉线。
		List<CardModel> powerPool = CardFactory
			.FilterForCombat(Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint))
			.Where(static card => card.Type == CardType.Power && card.CanBeGeneratedByModifiers)
			.ToList();
		if (powerPool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			powerPool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"singularity-ai-player-power",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			CountOwnedCardsDrawnFromHistory().ToString());
		CardModel card = combatState.CreateCard(canonicalCard, Owner);

		card.SetToFreeThisTurn();
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

namespace HextechRunes;

public sealed class SnakebiteRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Snakebite>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	// 额外回合不推进 RoundNumber 且会重新抽手牌,按 RoundNumber 防重(否则再送一张蛇咬)。
	private int _lastProcRound = -1;

	public override Task BeforeCombatStart()
	{
		_lastProcRound = -1;
		return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| combatState.RoundNumber > 1
			|| _lastProcRound == combatState.RoundNumber)
		{
			return;
		}

		_lastProcRound = combatState.RoundNumber;
		CardModel card = combatState.CreateCard<Snakebite>(Owner);
		card.SetToFreeThisCombat();
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}

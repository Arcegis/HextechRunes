namespace HextechRunes;

public sealed class SoulCallingRune : HextechRelicBase
{
	private bool _addedThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Soul>()
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedAddedThisCombat
	{
		get => _addedThisCombat;
		set => _addedThisCombat = value;
	}

	public override Task BeforeCombatStart()
	{
		_addedThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_addedThisCombat = false;
		return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (_addedThisCombat || player != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		_addedThisCombat = true;
		Flash();
		await AddSoulToPile(Owner, combatState, PileType.Draw);
		await AddSoulToPile(Owner, combatState, PileType.Hand);
		await AddSoulToPile(Owner, combatState, PileType.Discard);
	}

	private Task AddSoulToPile(Player owner, HextechCombatState combatState, PileType pileType)
	{
		IEnumerable<Soul> souls = Soul.Create(owner, DynamicVars.Cards.IntValue, combatState);
		return HextechCardGeneration.AddGeneratedCardsToCombat(
			souls,
			pileType,
			addedByPlayer: true,
			position: CardPilePosition.Top);
	}
}

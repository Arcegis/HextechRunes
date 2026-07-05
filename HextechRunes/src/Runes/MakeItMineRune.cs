namespace HextechRunes;

public sealed class MakeItMineRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private int _stacks;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStacks
	{
		get => _stacks;
		set
		{
			_stacks = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _stacks : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(4m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash();
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
#else
	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		if (player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState?.RoundNumber > 1
			|| _stacks <= 0
			|| !IsNecrobinderPlayer(player))
		{
			return;
		}

		Flash();
		await OstyCmd.Summon(choiceContext, player, _stacks * DynamicVars.Summon.BaseValue, this);
	}
}

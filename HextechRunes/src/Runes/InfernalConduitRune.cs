namespace HextechRunes;

public sealed class InfernalConduitRune : HextechRelicBase
{
	private int _pendingEnergy;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingEnergy
	{
		get => _pendingEnergy;
		set => _pendingEnergy = Math.Max(0, value);
	}

	public override Task BeforeCombatStart()
	{
		_pendingEnergy = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingEnergy = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null || target.Side != CombatSide.Enemy || !IsAttackDamageForRuneEffects(props, cardSource) || !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		await PowerCmd.Apply<HextechBurnPower>(target, 2m, Owner.Creature, cardSource);
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		_pendingEnergy = Owner.Creature.CombatState.Enemies
			.Where(enemy => enemy.IsAlive)
			.Sum(enemy => Math.Max(0, enemy.GetPowerAmount<HextechBurnPower>()) / 5);
		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || _pendingEnergy <= 0)
		{
			return;
		}

		int energy = _pendingEnergy;
		_pendingEnergy = 0;
		Flash();
		await PlayerCmd.GainEnergy(energy, player);
	}
}

namespace HextechRunes;

public sealed class SpeedDemonRune : TurnScopedRelicBase
{
	// 文案写的是字面值，改数值要同步九语言。
	private const decimal CardsToDraw = 2m;

	private bool _triggeredThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			UpdateTurnScopedStateIdentity(null);
		}
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent();
		if (Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| (!IsOwnerOrPet(dealer) && cardSource?.Owner != Owner)
			|| !TryConsumeTurnProc(nameof(SpeedDemonRune), ref _triggeredThisTurn))
		{
			return;
		}

		Flash([target]);
		await CardPileCmd.Draw(choiceContext, CardsToDraw, Owner);
	}

	protected override void ResetTurnScopedState()
	{
		_triggeredThisTurn = false;
	}
}

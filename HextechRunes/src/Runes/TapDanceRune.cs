namespace HextechRunes;

public sealed class TapDanceRune : HextechRelicBase
{
	private int _pendingDraw;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingDraw
	{
		get => _pendingDraw;
		set
		{
			_pendingDraw = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => false;

	public override int DisplayAmount => 0;

	public override Task BeforeCombatStart()
	{
		_pendingDraw = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingDraw = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CardPileCmd.Draw(context, 1m, Owner!, fromHandDraw: false);
	}
}

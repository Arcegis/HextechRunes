namespace HextechRunes;

public sealed class ShrinkEngineRune : HextechSharedCombatVictoryRuneBase
{
	// 每层缩小体型、每若干层多抽 1 张 / 多 1 点能量；文案写的是字面值，改数值要同步九语言。
	private const float BodyScaleStepPerStack = 0.02f;
	private const decimal StacksPerExtraDraw = 4m;
	private const decimal StacksPerExtraEnergy = 8m;

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

	internal float BodyScaleDelta => -_stacks * BodyScaleStepPerStack;

	public override Task AfterObtained()
	{
		Shrink();
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Shrink();
		return Task.CompletedTask;
	}

	public override Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash(Array.Empty<Creature>());
		Shrink();
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + FloorToInt(_stacks / StacksPerExtraDraw) : count;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? amount + FloorToInt(_stacks / StacksPerExtraEnergy) : amount;
	}

	private void Shrink()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
	}
}

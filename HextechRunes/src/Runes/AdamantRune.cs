using System.Diagnostics.CodeAnalysis;

namespace HextechRunes;

// 基类沿用"负面效果触发"的那一套：它的 SavedProcsThisTurn 在 SavedProperty 清单里，换基类会改变保存与联机布局。
public sealed class AdamantRune : LimitedDebuffProcRelicBase
{
	protected override bool HasTurnLimit => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(4m, ValueProp.Unpowered)
	];

	protected override bool TryMatchProc(PowerModel power, decimal amount, Creature? applier, [NotNullWhen(true)] out Creature? target)
	{
		return TryGetOwnerReceivedBuff(power, amount, out target);
	}

	protected override Task OnDebuffProc(Player owner, Creature target)
	{
		return CreatureCmd.GainBlock(owner.Creature, DynamicVars.Block, null);
	}
}

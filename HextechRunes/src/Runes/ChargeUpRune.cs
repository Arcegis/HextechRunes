namespace HextechRunes;

// 蓄能(仅储君) —— 当你花费辉星时,获得相同数值的活力(VigorPower)。
public sealed class ChargeUpRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>()
	];

	public override bool IsAvailableForPlayer(Player player) => IsRegentPlayer(player);

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		if (Owner == null || Owner.Creature.IsDead || spender != Owner || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Creature]);
		return PowerCmd.Apply<VigorPower>(Owner.Creature, amount, Owner.Creature, null);
	}
}

namespace HextechRunes;

public sealed class RoyaltiesUpgradeRune : CardUpgradeRuneBase<Royalties>
{
	protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);

	// 旧版本存档兼容占位：原为待发放的战后金币计数，金币已改为触发时立即发放；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCountThisCombat
	{
		get => 0;
		set { }
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == Owner && player.Creature.CombatState != null)
		{
			// 不改王国资产层数，额外金币不参与复利；同步回调由各端分别等待原版命令。
			int gold = player.Creature.GetPowerAmount<RoyaltiesPower>();
			if (gold > 0)
			{
				return PlayerCmd.GainGold(gold, player);
			}
		}

		return Task.CompletedTask;
	}
}

namespace HextechRunes;

public sealed class HomeguardRune : HextechRelicBase
{
	private bool _tookUnblockedDamageSinceLastTurn;
	private bool _hasPreviousTurn;

	// (C/rank1)联机下不把"本回合是否受未格挡伤害/是否有上一回合"这两个本地推算 bool 写进 checksum:
	// getter 在 IsNetworkMultiplayer() 返回类型默认值(false)→ SaveIfNotTypeDefault 下根本不序列化 → 退出
	// NetFullCombatState 校验,避免上游(掉线/超时/rejoin)的一次瞬时跨端差异被这两个无归一化的 checksummed bool
	// 永久锁存成 "After player turn start" 分叉。对齐本 mod 既有计数鲁恩(SwiftAndSafe/Nightstalking)的 MP 安全约定。
	// 单机仍用本地字段;字段本身仍驱动抽牌逻辑(正常 lockstep 下各端一致),此处仅切断"进 checksum"这条路。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTookUnblockedDamageSinceLastTurn
	{
		get => IsNetworkMultiplayer() ? false : _tookUnblockedDamageSinceLastTurn;
		set => _tookUnblockedDamageSinceLastTurn = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedHasPreviousTurn
	{
		get => IsNetworkMultiplayer() ? false : _hasPreviousTurn;
		set => _hasPreviousTurn = value;
	}

	public override Task BeforeCombatStart()
	{
		_tookUnblockedDamageSinceLastTurn = false;
		_hasPreviousTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_tookUnblockedDamageSinceLastTurn = false;
		_hasPreviousTurn = false;
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner != null && target == Owner.Creature && result.UnblockedDamage > 0)
		{
			_tookUnblockedDamageSinceLastTurn = true;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner)
		{
			return;
		}

		if (_hasPreviousTurn && !_tookUnblockedDamageSinceLastTurn)
		{
			Flash();
			await CardPileCmd.Draw(choiceContext, 2m, player);
		}

		_hasPreviousTurn = true;
		_tookUnblockedDamageSinceLastTurn = false;
	}
}

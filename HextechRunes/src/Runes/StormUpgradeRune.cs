namespace HextechRunes;

// 升级雷暴：大乱斗中持有者的原版 StormPower 被整体跳过（HextechCombatHooks.PowerCompat 的两个前缀），
// 改由本符文让任意类型的牌都按「打出前」的 Storm 层数补发闪电。
//
// 算法在本类，但调用时机仍由 HextechMayhemModifier 的 BeforeCardPlayed / AfterCardPlayedLate 分发，
// 没有改成本遗物自己覆写这两个钩子：原版 Hook 按「生物 Power → 该玩家遗物 → 牌 → Modifier」顺序遍历监听者，
// 遗物覆写的 AfterCardPlayedLate 会让闪电提前到其他符文（奥术重击、恶魔之舞等）的联机补记与后续玩家的监听者之前，
// 而原实现一直在所有监听者之后才补发。改变结算先后属于玩法行为变化，这里保持原时机。
public sealed class StormUpgradeRune : CardUpgradeRuneBase<Storm>
{
	// 每张牌「开始打出时」持有者的 Storm 层数（在该牌 OnPlay 施加/叠加 StormPower 之前记录）。
	// 复刻原版 StormPower 的自排除：首次打出雷暴时还没有 StormPower → 不记录 → 不会对雷暴自己发闪电；
	// 后续打出雷暴发的也是「打出前」的层数。战斗内临时状态，不存档。
	private readonly Dictionary<CardModel, int> _stormLightningAtCardStart = new();

	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);

	// 原版 StormPower 只对能力牌触发；持有本符文时所有类型都触发。
	internal static bool ShouldTrigger(CardType cardType, bool hasUpgradeRune)
	{
		return cardType == CardType.Power || hasUpgradeRune;
	}

	public override Task BeforeCombatStart()
	{
		_stormLightningAtCardStart.Clear();
		return Task.CompletedTask;
	}

	// 出牌被打断时 AfterCardPlayedLate 不会来取，残留条目会跨战斗持有卡牌引用。
	public override Task AfterCombatEnd(CombatRoom room)
	{
		_stormLightningAtCardStart.Clear();
		return Task.CompletedTask;
	}

	internal static StormUpgradeRune? FindForCardPlay(CardPlay cardPlay, RunState runState)
	{
		Player? owner = cardPlay.Card.Owner;
		if (owner == null || owner.Creature.CombatState?.RunState != runState)
		{
			return null;
		}

		return owner.GetRelic<StormUpgradeRune>();
	}

	internal void RecordStormBeforeCardPlayed(CardPlay cardPlay)
	{
		if (Owner?.Creature.GetPower<StormPower>() is not StormPower stormPower)
		{
			return;
		}

		int lightning = Math.Max(0, (int)Math.Floor((decimal)stormPower.Amount));
		if (lightning > 0)
		{
			_stormLightningAtCardStart[cardPlay.Card] = lightning;
		}
	}

	// 只对「打出前就已持有 Storm」的牌补发闪电；发的是打出前记录的层数。
	internal async Task ChannelRecordedLightningAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!_stormLightningAtCardStart.Remove(cardPlay.Card, out int lightningCount) || lightningCount <= 0)
		{
			return;
		}

		Player? owner = Owner;
		if (owner == null)
		{
			return;
		}

		for (int i = 0; i < lightningCount; i++)
		{
			OrbModel orb = ModelDb.Orb<LightningOrb>().ToMutable();
			await OrbCmd.Channel(choiceContext, orb, owner);
		}
	}
}

namespace HextechRunes;

// 升级：狂宴(仅战士) —— 用狂宴(Feed)斩杀敌人时,额外获得 15% 最大生命加成。
// 加成按"基础最大生命"(当前最大生命 - 本符文历次已加总量)计算,即加法叠加:
// 触发两次系数为 1.3 而非乘法复利的 1.3225(复利在长局指数失控,玩家实报后改口径)。
public sealed class FeedUpgradeRune : CardUpgradeRuneBase<Feed>
{
	private const decimal BonusMaxHpPercent = 0.15m;

	private int _bonusGranted;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedFeedBonusGranted
	{
		get => _bonusGranted;
		set => _bonusGranted = Math.Max(0, value);
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Feed>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not Feed
			|| cardPlay.Target is not { IsDead: true })
		{
			return;
		}

		decimal baseMaxHp = Math.Max(1m, Owner.Creature.MaxHp - _bonusGranted);
		int bonus = Math.Max(1, (int)Math.Floor(baseMaxHp * BonusMaxHpPercent));
		_bonusGranted += bonus;
		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, bonus);
	}
}

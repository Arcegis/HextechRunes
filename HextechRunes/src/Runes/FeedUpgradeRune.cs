namespace HextechRunes;

// 升级：狂宴(仅战士) —— 用狂宴(Feed)斩杀敌人时,额外获得 15% 当前最大生命值的最大生命加成。
public sealed class FeedUpgradeRune : CardUpgradeRuneBase<Feed>
{
	private const decimal BonusMaxHpPercent = 0.15m;

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

		int bonus = Math.Max(1, (int)Math.Floor(Owner.Creature.MaxHp * BonusMaxHpPercent));
		Flash();
		await CreatureCmd.GainMaxHp(Owner.Creature, bonus);
	}
}

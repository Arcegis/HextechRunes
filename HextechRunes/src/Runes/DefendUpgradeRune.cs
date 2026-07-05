namespace HextechRunes;

// 升级：防御 —— 打出基础防御(任意角色)后,把它在本场战斗中变化为妙计(Finesse)。
// 防御无统一卡基类(DefendIronclad… : CardModel),按 CardTag.Defend + 基础稀有度识别。转化时机/安全性见基类。
public sealed class DefendUpgradeRune : TransformBasicCardOnPlayRuneBase<Finesse>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Finesse>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return player.Deck.Cards.Any(IsBasicDefend);
	}

	protected override bool ShouldTransform(CardModel card) => IsBasicDefend(card);

	private static bool IsBasicDefend(CardModel card)
	{
		return card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Defend);
	}
}

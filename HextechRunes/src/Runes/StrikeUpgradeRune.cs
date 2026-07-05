namespace HextechRunes;

// 升级：打击 —— 打出基础打击(任意角色)后,把它在本场战斗中变化为亮剑(FlashOfSteel)。
// 打击无统一卡基类(StrikeIronclad… : CardModel),按 CardTag.Strike + 基础稀有度识别。转化时机/安全性见基类。
public sealed class StrikeUpgradeRune : TransformBasicCardOnPlayRuneBase<FlashOfSteel>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<FlashOfSteel>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return player.Deck.Cards.Any(IsBasicStrike);
	}

	protected override bool ShouldTransform(CardModel card) => IsBasicStrike(card);

	private static bool IsBasicStrike(CardModel card)
	{
		return card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike);
	}
}

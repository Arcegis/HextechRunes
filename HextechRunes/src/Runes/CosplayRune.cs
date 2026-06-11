using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class CosplayRune : HextechRelicBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(Lantern)
	];

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<FeelNoPain>(),
		HoverTipFactory.FromCard<Juggernaut>(),
		HoverTipFactory.FromCard<Fuel>(),
		HoverTipFactory.FromCard<BattleTrance>(),
		HoverTipFactory.FromKeyword(CardKeyword.Innate),
		.. HoverTipFactory.FromRelic<Lantern>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await AddInnateCard<FeelNoPain>();
		await AddInnateCard<Juggernaut>();
		await AddInnateCard<Fuel>();
		await AddInnateCard<BattleTrance>();
		await RelicBundleGrantHelper.GrantRelics(Owner, RelicTypes);
	}

	private Task AddInnateCard<TCard>()
		where TCard : CardModel
	{
		return AddCardCopiesToDeckOrHand<TCard>(1, static card =>
		{
			if (!card.Keywords.Contains(CardKeyword.Innate))
			{
				card.AddKeyword(CardKeyword.Innate);
			}
		});
	}
}

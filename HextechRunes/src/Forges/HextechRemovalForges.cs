using MegaCrit.Sts2.Core.CardSelection;

namespace HextechRunes;

// 剪刀锻造器：获得时从牌组移除若干张牌（张数由子类的 CardsVar 给出），流程同原版精准剪刀。
// 叠层（再次获得同名锻造器）会再移除一次。
public abstract class CardRemovalForgeBase : HextechForgeBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		CardSelectorPrefs prefs = new(CardSelectorPrefs.RemoveSelectionPrompt, DynamicVars.Cards.IntValue);
		List<CardModel> selected = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList();
		if (selected.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in selected)
		{
			await CardPileCmd.RemoveFromDeck(card);
		}
	}
}

public sealed class ScissorsForge : CardRemovalForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];
}

public sealed class GoldScissorsForge : CardRemovalForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];
}

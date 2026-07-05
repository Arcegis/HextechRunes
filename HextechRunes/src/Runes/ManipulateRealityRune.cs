using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class ManipulateRealityRune : HextechRelicBase
{
#if STS2_104_OR_NEWER
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
#else
	public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
#endif
	{
		if (card.Owner != Owner || !card.IsUpgradable)
		{
			return Task.CompletedTask;
		}

		CardCmd.Upgrade(card, CardPreviewStyle.None);
		Flash();
		return Task.CompletedTask;
	}
}

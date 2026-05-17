using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

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

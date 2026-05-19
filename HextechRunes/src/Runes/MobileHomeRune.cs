using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class MobileHomeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	private static readonly Type[] RelicTypes =
	[
		typeof(MeatCleaver),
		typeof(Shovel),
		typeof(Girya),
		typeof(MiniatureTent)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromRelic<MeatCleaver>(),
		.. HoverTipFactory.FromRelic<Shovel>(),
		.. HoverTipFactory.FromRelic<Girya>(),
		.. HoverTipFactory.FromRelic<MiniatureTent>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await RelicBundleGrantHelper.GrantRelics(Owner, RelicTypes);
	}
}

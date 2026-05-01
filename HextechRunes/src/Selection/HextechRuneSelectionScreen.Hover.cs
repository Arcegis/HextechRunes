using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.addons.mega_text;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen : Control, IOverlayScreen, IScreenContext
{
	private static void AttachRelicHoverTips(Control holder, RelicModel relic)
	{
		holder.MouseFilter = MouseFilterEnum.Pass;
		holder.MouseDefaultCursorShape = CursorShape.Help;
		holder.MouseEntered += () => ShowRelicHoverTips(holder, relic);
		holder.MouseExited += () => NHoverTipSet.Remove(holder);
		holder.TreeExiting += () => NHoverTipSet.Remove(holder);
	}

	private static void ShowRelicHoverTips(Control holder, RelicModel relic)
	{
		NHoverTipSet.Remove(holder);
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(holder, relic.HoverTips, HoverTip.GetHoverTipAlignment(holder));
		hoverTipSet?.SetAlignment(holder, HoverTip.GetHoverTipAlignment(holder));
	}
}

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
	private const string LocTable = "relic_collection";
	private const string RerollIconPath = "res://HextechRunes/images/ui/hextechReroll.png";

	private readonly TaskCompletionSource<IEnumerable<RelicModel>> _completionSource = new();
	private readonly Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? _rerollFunc;
	private List<RelicModel> _relics;
	private readonly RelicModel? _monsterHexRelic;
	private readonly string _rarityKey;
	private readonly List<Button> _holders = new();
	private readonly List<Button> _rerollButtons = new();
	private readonly List<bool> _rerolledSlots = new();
	private readonly List<int> _rerollHistory = new();
	private HBoxContainer? _cardsRow;
	private MegaLabel? _statusLabel;
	private bool _choiceLocked;
	private bool _restoreAfterMapReopenQueued;
	private bool _closed;

	public NetScreenType ScreenType => NetScreenType.Rewards;

	public bool UseSharedBackstop => true;

	public Control? DefaultFocusedControl => _holders.FirstOrDefault();

	public bool RequestedReroll => false;

	public IReadOnlyList<RelicModel> CurrentRelics => _relics;

	public IReadOnlyList<int> RerollHistory => _rerollHistory;

	private HextechRuneSelectionScreen(IReadOnlyList<RelicModel> relics, RelicModel? monsterHexRelic, Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? rerollFunc)
	{
		_relics = relics.ToList();
		_monsterHexRelic = monsterHexRelic;
		_rerollFunc = rerollFunc;
		_rarityKey = DetermineRarityKey(relics);
		Name = nameof(HextechRuneSelectionScreen);
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		FocusMode = FocusModeEnum.All;
		FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Enabled;
		Visible = true;
		BuildUi();
	}

	public static HextechRuneSelectionScreen Create(IReadOnlyList<RelicModel> relics, RelicModel? monsterHexRelic, Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? rerollFunc = null)
	{
		Log.Info($"[{ModInfo.Id}][Mayhem] SelectionScreen.Create: count={relics.Count}");
		return new HextechRuneSelectionScreen(relics, monsterHexRelic, rerollFunc);
	}

	public override void _ExitTree()
	{
		_completionSource.TrySetResult(Array.Empty<RelicModel>());
		base._ExitTree();
	}
}

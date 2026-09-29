using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

internal static partial class HextechRelicVisibilityHooks
{
	// 兼容入口:偏好本体在 HextechUiPreferences,这两个读取口仍被选择界面与更新检查使用。
	internal static bool GetShowUpdateNotice()
	{
		return HextechUiPreferences.ShowUpdateNotice;
	}

	internal static bool GetConfirmRuneSelection()
	{
		return HextechUiPreferences.ConfirmRuneSelection;
	}

	/// <summary>「显示隐藏遗物开关」偏好改变后,在当前局的界面上立即装上或拆掉开关并刷新隐藏状态。</summary>
	internal static void RefreshToggleForCurrentRun()
	{
		NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
		if (globalUi != null && GodotObject.IsInstanceValid(globalUi))
		{
			InstallToggle(globalUi);
			ApplyHiddenState(globalUi);
		}
	}
}

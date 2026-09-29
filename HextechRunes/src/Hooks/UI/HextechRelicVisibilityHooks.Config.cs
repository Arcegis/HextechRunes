using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

internal static partial class HextechRelicVisibilityHooks
{
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

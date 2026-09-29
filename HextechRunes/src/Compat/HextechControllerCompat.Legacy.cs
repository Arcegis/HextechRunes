#if STS2_107_1
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

internal static class HextechControllerCompat
{
	// 0.107.1 只有手柄/鼠标两种模式,没有纯键盘方向导航;以"正在用手柄"代替。
	internal static bool IsUsingDirectionalNavigation(NControllerManager? manager) => manager?.IsUsingController == true;
}
#endif

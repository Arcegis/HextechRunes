using Godot;

namespace HextechRunes;

/// <summary>
/// 挂在原版界面里(不在 <see cref="HextechControllerOverlay"/> 之内)的自绘按钮。手柄确认键是原版的 <c>ui_select</c>,
/// Godot 按钮只认 <c>ui_accept</c>;获得焦点时把 <c>ui_select</c> 翻译成 <c>ui_accept</c>,与覆盖层走同一套逻辑。
/// </summary>
internal sealed partial class HextechSelectAcceptButton : Button
{
	public override void _Input(InputEvent inputEvent)
	{
		if (IsVisibleInTree() && HasFocus())
		{
			HextechControllerInput.TryTranslateSelectToAccept(this, inputEvent);
		}
	}
}

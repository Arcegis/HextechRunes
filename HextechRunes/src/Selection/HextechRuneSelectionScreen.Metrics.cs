using Godot;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen
{
	private const string LocTable = "relic_collection";
	private const string RerollIconPath = "res://HextechRunes/images/ui/hextechReroll.png";

	private static readonly Vector2 PlayerRuneCardSize = new(344f, 592f);
	private const int PlayerRuneCardBottomMargin = 112;
	private static readonly Vector2 PlayerRerollButtonSize = new(144f, 76f);
	private const float PlayerRerollButtonBottomInset = 24f;
	private const float PlayerRerollIconSize = 44f;
}

using Godot;

namespace HextechRunes;

internal static class HextechGodotAsync
{
	internal static async Task<bool> AwaitProcessFrameAsync(Node node)
	{
		if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
		{
			return false;
		}

		await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
		return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
	}
}

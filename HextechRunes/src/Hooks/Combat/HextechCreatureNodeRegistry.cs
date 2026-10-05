using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

/// <summary>entity → 屏幕节点映射,由战斗节点生命周期 hook 填充;新战斗重建,取用时校验有效性。</summary>
internal static class HextechCreatureNodeRegistry
{
	private static readonly Dictionary<Creature, NCreature> Nodes = new();

	internal static void Clear()
	{
		Nodes.Clear();
	}

	internal static void Register(NCreature? node)
	{
		if (!GodotObject.IsInstanceValid(node) || node!.Entity == null)
		{
			return;
		}

		Nodes[node.Entity] = node;
	}

	// 原版 GetCreatureNode 只是在节点列表里按 Entity 查找，不会抛异常。
	internal static NCreature? SafeGetCreatureNode(NCombatRoom room, Creature creature)
	{
		return room.GetCreatureNode(creature);
	}

	internal static NCreature? TryGet(Creature? creature)
	{
		if (creature != null && Nodes.TryGetValue(creature, out NCreature? node) && GodotObject.IsInstanceValid(node))
		{
			return node;
		}

		return null;
	}
}

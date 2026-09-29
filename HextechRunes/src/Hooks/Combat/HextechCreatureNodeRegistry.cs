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

	/// <summary>AddCreature postfix 专用:GetCreatureNode 在战斗构建/召唤同步链上,异常不能外泄。</summary>
	internal static NCreature? SafeGetCreatureNode(NCombatRoom room, Creature creature)
	{
		try
		{
			return room.GetCreatureNode(creature);
		}
		catch (Exception ex)
		{
			if (HextechRunLogBudget.TryConsume("combat.creature-node-safe-get-failure", 5))
			{
				HextechLog.Error("Mayhem", $"GetCreatureNode failed in AddCreature postfix: {ex}");
			}

			return null;
		}
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

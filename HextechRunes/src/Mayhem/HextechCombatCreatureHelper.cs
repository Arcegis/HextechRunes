using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechCombatCreatureHelper
{
	public static void DowngradePlayerCombatCards(HextechCombatState combatState)
	{
		foreach (CardModel card in combatState.Players
			.SelectMany(static player => player.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>())
			.Where(static card => card.IsUpgraded)
			.ToList())
		{
			CardCmd.Downgrade(card);
		}
	}

	public static IReadOnlyList<Creature> GetAliveEnemies(HextechCombatState combatState)
	{
		return combatState.Enemies.Where(static creature => creature.IsAlive).ToList();
	}

	public static IReadOnlyList<Creature> GetAlivePlayerSideCreatures(HextechCombatState combatState)
	{
		return combatState.PlayerCreatures.Where(static creature => creature.IsAlive).ToList();
	}

	public static void RemoveRetainedDeadEnemyIfNeeded(HextechCombatState combatState, Creature enemy)
	{
		if (enemy.Side != CombatSide.Enemy
			|| enemy.IsAlive
			|| !combatState.Enemies.Contains(enemy)
			|| !Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, enemy))
		{
			return;
		}

		var node = NCombatRoom.Instance?.GetCreatureNode(enemy);
		if (node != null)
		{
			NCombatRoom.Instance?.RemoveCreatureNode(node);
		}

		CombatManager.Instance.RemoveCreature(enemy);
		combatState.RemoveCreature(enemy);
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] Removed retained dead enemy after unsafe PainfulStabs cleanup: id={enemy.CombatId?.ToString() ?? "none"} model={enemy.ModelId.Entry}");
	}
}

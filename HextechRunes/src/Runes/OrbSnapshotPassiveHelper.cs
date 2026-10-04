namespace HextechRunes;

internal static class OrbSnapshotPassiveHelper
{
	// 按调用方在触发开始时冻结的球快照逐轮触发：中途离开球栏的球跳过，结算中新生成的球不扩大这次结算。
	// 每次触发前重查战斗结束和持有者死亡，命令链可能在任意一次被动里结束战斗。
	internal static async Task TriggerRounds(Player player, IReadOnlyList<OrbModel> orbs, int rounds, Func<OrbModel, Task> trigger)
	{
		for (int round = 0; round < rounds; round++)
		{
			foreach (OrbModel orb in orbs)
			{
				if (CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead || player.PlayerCombatState == null)
				{
					return;
				}

				if (player.PlayerCombatState.OrbQueue.Orbs.Contains(orb))
				{
					await trigger(orb);
				}
			}
		}
	}
}

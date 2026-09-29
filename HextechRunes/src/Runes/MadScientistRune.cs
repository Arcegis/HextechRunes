using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

public sealed class MadScientistRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("OrbSlots", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int orbSlots = Math.Max(0, DynamicVars["OrbSlots"].IntValue);
		if (orbSlots <= 0)
		{
			return;
		}

		Flash();
		await OrbCmd.AddSlots(Owner, orbSlots);
	}

	// 跳过理由：原版 OrbCmd.AddSlots(三个版本一致)写死 amount = Min(10 - Capacity, amount)，充能球栏位上限 10
	// 没有任何 Hook 可改。激活条件：仅对持有本符文的玩家替换为不封顶的同一流程(IsOverOrEnding 判断 → AddCapacity →
	// 栏位动画)，其他玩家走原版；战斗外(PlayerCombatState 为空)回落原版。原方法 IL 由 vanilla_copy_guard 冻结。
	[HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.AddSlots), typeof(Player), typeof(int))]
	[HextechPatch("rune.mad-scientist", "科学狂人", Rune = typeof(MadScientistRune))]
	private static class MadScientistPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(Player player, int amount, ref Task __result)
		{
			if (player.GetRelic<MadScientistRune>() == null)
			{
				return true;
			}

			if (CombatManager.Instance.IsOverOrEnding || amount <= 0)
			{
				__result = Task.CompletedTask;
				return false;
			}

			if (player.PlayerCombatState == null)
			{
				return true;
			}

			player.PlayerCombatState.OrbQueue.AddCapacity(amount);
			try
			{
				NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddSlotAnim(amount);
			}
			catch (Exception ex)
			{
				HextechLog.Warn("MadScientist", $"Orb slot visual failed: {ex.Message}");
			}
			__result = Task.CompletedTask;
			return false;
		}
	}
}

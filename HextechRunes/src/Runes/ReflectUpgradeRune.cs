namespace HextechRunes;

public sealed class ReflectUpgradeRune : CardUpgradeRuneBase<Reflect>
{
	protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);

	// 跳过理由：原版 ReflectPower.AfterSideTurnStart(三个版本一致)在持有者回合开始时 PowerCmd.Decrement(this)。
	// Decrement 虽经 PowerCmd.ModifyAmount 走 Hook.ModifyPowerAmountReceived(offset=-1、applier=null，0.111.0 反编译)，
	// 但该 Hook 区分不出"回合开始衰减"和其他同形态扣减，且仍会写入 History.PowerReceived、触发 BeforePowerAmountChanged；
	// 所以直接跳过这一次衰减。
	// 激活条件：仅倒映持有者拥有本符文时跳过，其余玩家走原版。原方法 IL 由 vanilla_copy_guard 冻结。
	[HarmonyPatch(typeof(ReflectPower), nameof(ReflectPower.AfterSideTurnStart))]
	[HextechPatch("rune.reflect.persistent", "升级倒映", Rune = typeof(ReflectUpgradeRune))]
	private static class PersistentReflectPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		internal static bool Prefix(ReflectPower __instance, ref Task __result)
		{
			if (__instance.Owner.Player?.GetRelic<ReflectUpgradeRune>() == null)
			{
				return true;
			}

			__result = Task.CompletedTask;
			return false;
		}
	}
}

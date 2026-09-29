namespace HextechRunes;

// 早期版本间 CreatureCmd.SetMaxHp 的返回类型不同（Task / Task<decimal>），这里曾用反射转发。
// 已核对当前三个发布目标 0.107.1、0.110.0、0.111.0 的反编译，签名都是 public static Task<decimal> SetMaxHp(Creature, decimal)，
// 所以改为直接调用。保留这个入口，调用方不关心新的最大生命返回值。
internal static class CreatureCmdCompat
{
	internal static Task SetMaxHp(Creature creature, decimal amount)
	{
		return CreatureCmd.SetMaxHp(creature, amount);
	}
}

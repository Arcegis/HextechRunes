using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	private static List<MethodInfo> ResolveGetScaledAmountForMultiplayerTargets()
	{
		List<MethodInfo> targets = [];
		foreach (Type powerType in ScalingOverrides.Keys)
		{
			if (TryGetMethod(
					powerType,
					nameof(PowerModel.GetScaledAmountForMultiplayer),
					BindingFlags.Public | BindingFlags.Instance,
					warnIfMissing: false,
					typeof(HextechCombatState),
					typeof(Creature),
					typeof(decimal),
					typeof(Creature),
					typeof(CardModel)) is not MethodInfo method)
			{
				continue;
			}

			// 从派生类型取到的 MethodInfo 以该派生类型为 ReflectedType；经句柄取回声明处的实例，
			// 没覆写的类型才会归并成 PowerModel 上的同一个目标，不会对同一方法重复安装。
			MethodInfo declared = (MethodInfo)MethodBase.GetMethodFromHandle(method.MethodHandle)!;
			if (!targets.Contains(declared))
			{
				targets.Add(declared);
			}
		}

		return targets;
	}
}

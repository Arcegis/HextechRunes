using HarmonyLib;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{

	/// <summary>
	/// 只补原版程序集里的充能球 Evoke 覆写。以前会扫描所有已加载程序集并给第三方模组的充能球类也打补丁,
	/// 那等于替别人的类型做决定;第三方充能球现在保持原版激发,亮剑不替换它们。
	/// </summary>
	internal static IReadOnlyList<MethodInfo> FindLoadedOrbEvokeMethods()
	{
		Assembly coreAssembly = typeof(OrbModel).Assembly;
		HashSet<MethodInfo> methods =
		[
			typeof(OrbModel).GetMethod(
				nameof(OrbModel.Evoke),
				BindingFlags.Instance | BindingFlags.Public,
				binder: null,
				types: [typeof(PlayerChoiceContext)],
				modifiers: null)
				?? throw new MissingMethodException(typeof(OrbModel).FullName, nameof(OrbModel.Evoke))
		];

		foreach (Type type in GetLoadableTypes(coreAssembly))
		{
			if (type == typeof(OrbModel) || !typeof(OrbModel).IsAssignableFrom(type))
			{
				continue;
			}

			MethodInfo? evoke = type.GetMethod(
				nameof(OrbModel.Evoke),
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
				binder: null,
				types: [typeof(PlayerChoiceContext)],
				modifiers: null);
			if (evoke is { IsAbstract: false } && evoke.ReturnType == typeof(Task<IEnumerable<Creature>>))
			{
				methods.Add(evoke);
			}
		}

		return methods
			.OrderBy(static method => method.DeclaringType?.FullName, StringComparer.Ordinal)
			.ToArray();
	}

	internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(static type => type != null).Cast<Type>();
		}
	}

	internal static bool OrbEvokePrefix(OrbModel __instance, ref Task<IEnumerable<Creature>> __result)
	{
		DrawYourSwordRune? rune = __instance.Owner?.GetRelic<DrawYourSwordRune>();
		if (rune == null || !rune.ShouldReplaceOrbEvoke(__instance))
		{
			return true;
		}

		__result = rune.ReplaceOrbEvoke();
		return false;
	}

}

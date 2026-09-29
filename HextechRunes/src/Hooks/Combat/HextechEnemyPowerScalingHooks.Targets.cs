using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechEnemyPowerScalingHooks
{
	private static List<MethodInfo> ResolveGetScaledAmountForMultiplayerTargets()
	{
		List<MethodInfo> targets = [];
		foreach (Type powerType in ScalingOverrides.Keys)
		{
			MethodInfo? method = TryGetMethod(
				powerType,
				nameof(PowerModel.GetScaledAmountForMultiplayer),
				BindingFlags.Public | BindingFlags.Instance,
				warnIfMissing: false,
				typeof(HextechCombatState),
				typeof(Creature),
				typeof(decimal),
				typeof(Creature),
				typeof(CardModel));
			if (method == null)
			{
				continue;
			}

			Type declaringType = method.DeclaringType ?? typeof(PowerModel);
			method = TryGetMethod(
				declaringType,
				nameof(PowerModel.GetScaledAmountForMultiplayer),
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
				warnIfMissing: false,
				typeof(HextechCombatState),
				typeof(Creature),
				typeof(decimal),
				typeof(Creature),
				typeof(CardModel));
			if (method == null)
			{
				continue;
			}

			if (!ContainsMethod(targets, method))
			{
				targets.Add(method);
			}
		}

		return targets;
	}

	private static bool ContainsMethod(IEnumerable<MethodInfo> methods, MethodInfo candidate)
	{
		foreach (MethodInfo method in methods)
		{
			if (method.Module == candidate.Module && method.MetadataToken == candidate.MetadataToken)
			{
				return true;
			}
		}

		return false;
	}
}

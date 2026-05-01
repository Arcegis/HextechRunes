using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Singleton;

namespace HextechRunes;

internal static class HextechEnemyPowerScalingHooks
{
	private enum ScalingOverride
	{
		Unscaled,
		PlayerCount
	}

	private static readonly AsyncLocal<ScalingOverride?> CurrentOverride = new();

	public static void Install(Harmony harmony)
	{
		HarmonyMethod prefix = new(typeof(HextechEnemyPowerScalingHooks), nameof(ModifyPowerAmountGivenPrefix))
		{
			priority = Priority.First
		};

		harmony.Patch(ResolveModifyPowerAmountGivenTarget(), prefix: prefix);
	}

	public static async Task<T?> Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
		where T : PowerModel
	{
		ScalingOverride? scalingOverride = GetScalingOverride(typeof(T));
		if (scalingOverride == null)
		{
			return await PowerCmd.Apply<T>(target, amount, applier, cardSource, silent);
		}

		using (BeginOverride(scalingOverride.Value))
		{
			return await PowerCmd.Apply<T>(target, amount, applier, cardSource, silent);
		}
	}

	private static bool ModifyPowerAmountGivenPrefix(
		PowerModel power,
		Creature giver,
		decimal amount,
		Creature? target,
		CardModel? cardSource,
		ref decimal __result)
	{
		ScalingOverride? scalingOverride = CurrentOverride.Value;
		if (scalingOverride == null
			|| target == null
			|| (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
			|| GetScalingOverride(power.GetType()) != scalingOverride)
		{
			return true;
		}

		__result = scalingOverride.Value switch
		{
			ScalingOverride.PlayerCount => amount * Math.Max(1, GetPlayerCount(giver, target)),
			ScalingOverride.Unscaled => amount,
			_ => amount
		};
		return false;
	}

	private static ScalingOverride? GetScalingOverride(Type powerType)
	{
		if (powerType == typeof(ArtifactPower) || powerType == typeof(SlipperyPower))
		{
			return ScalingOverride.PlayerCount;
		}

		if (powerType == typeof(HardenedShellPower) || powerType == typeof(RegenPower) || powerType == typeof(PlatingPower))
		{
			return ScalingOverride.Unscaled;
		}

		return null;
	}

	private static int GetPlayerCount(Creature giver, Creature target)
	{
		return target.CombatState?.Players.Count
			?? giver.CombatState?.Players.Count
			?? 1;
	}

	private static OverrideScope BeginOverride(ScalingOverride scalingOverride)
	{
		return new OverrideScope(scalingOverride);
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}

	private static MethodInfo ResolveModifyPowerAmountGivenTarget()
	{
		MethodInfo reflectedMethod = RequireMethod(
			typeof(MultiplayerScalingModel),
			nameof(MultiplayerScalingModel.ModifyPowerAmountGiven),
			BindingFlags.Public | BindingFlags.Instance,
			typeof(PowerModel),
			typeof(Creature),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel));

		if (reflectedMethod.DeclaringType == typeof(MultiplayerScalingModel)
			&& reflectedMethod.GetMethodBody() != null)
		{
			return reflectedMethod;
		}

		MethodInfo baseDefinition = reflectedMethod.GetBaseDefinition();
		if (baseDefinition.GetMethodBody() != null)
		{
			return baseDefinition;
		}

		Type declaringType = reflectedMethod.DeclaringType ?? typeof(AbstractModel);
		return RequireMethod(
			declaringType,
			nameof(AbstractModel.ModifyPowerAmountGiven),
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			typeof(PowerModel),
			typeof(Creature),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel));
	}

	private sealed class OverrideScope : IDisposable
	{
		private readonly ScalingOverride? _previousOverride;

		public OverrideScope(ScalingOverride scalingOverride)
		{
			_previousOverride = CurrentOverride.Value;
			CurrentOverride.Value = scalingOverride;
		}

		public void Dispose()
		{
			CurrentOverride.Value = _previousOverride;
		}
	}
}

using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SponsorModInfo = HextechRunesSponsorPack.ModInfo;

namespace HextechRunes;

internal static class IntegratedStrategyEventsBridge
{
	private const string AssemblyName = "IntegratedStrategyEvents";
	private const string ProphecyProjectionRelicTypeName = "IntegratedStrategyEvents.Relics.ProphecyProjectionRelic";
	private const string FinalChoraleTypeName = "IntegratedStrategyEvents.Encounters.FinalChorale";

	private static Type? ProphecyProjectionRelicType;
	private static Type? FinalChoraleType;

	internal static bool IsAvailable => TryResolveTypes(out _, out _);

	internal static bool IsFinalChorale(Creature creature)
	{
		if (creature.Monster?.GetType().FullName == FinalChoraleTypeName)
		{
			return true;
		}

		return TryResolveTypes(out _, out Type? finalChoraleType)
			&& creature.Monster != null
			&& finalChoraleType.IsInstanceOfType(creature.Monster);
	}

	internal static async Task<bool> ObtainProphecyProjection(Player owner, int? choraleHp = null)
	{
		if (!TryCreateProphecyProjection(out RelicModel? projection))
		{
			return false;
		}

		if (choraleHp.HasValue)
		{
			ConfigureProjectionChoraleHp(projection, choraleHp.Value);
		}

		await RelicCmd.Obtain(projection, owner);
		return true;
	}

	private static bool TryCreateProphecyProjection(out RelicModel projection)
	{
		projection = null!;
		if (!TryResolveTypes(out Type? projectionType, out _))
		{
			return false;
		}

		try
		{
			if (Activator.CreateInstance(projectionType) is not RelicModel relic)
			{
				Log.Warn($"[{SponsorModInfo.Id}] Failed to create ProphecyProjectionRelic: created instance is not a relic.", 2);
				return false;
			}

			projection = relic;
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] Failed to create ProphecyProjectionRelic: {ex.Message}", 2);
			return false;
		}
	}

	private static void ConfigureProjectionChoraleHp(RelicModel projection, int choraleHp)
	{
		int hp = Math.Max(1, choraleHp);
		bool ok = true;
		ok &= SetNonPublicProperty(projection, "SavedProphecyProjectionChoraleDefeated", false);
		ok &= SetNonPublicProperty(projection, "SavedProphecyProjectionChoraleRewardsGranted", false);
		ok &= SetNonPublicProperty(projection, "SavedProphecyProjectionUsesScaledChoraleHp", true);
		ok &= SetNonPublicProperty(projection, "SavedProphecyProjectionChoraleHp", hp);
		if (!ok)
		{
			Log.Warn($"[{SponsorModInfo.Id}] ProphecyProjectionRelic HP bonus was only partially configured.", 2);
		}
	}

	private static bool SetNonPublicProperty(object instance, string propertyName, object value)
	{
		PropertyInfo? property = instance.GetType().GetProperty(
			propertyName,
			BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo? setter = property?.GetSetMethod(nonPublic: true);
		if (setter == null)
		{
			return false;
		}

		try
		{
			setter.Invoke(instance, [value]);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] Failed to set {propertyName} on ProphecyProjectionRelic: {ex.Message}", 2);
			return false;
		}
	}

	private static bool TryResolveTypes(out Type prophecyProjectionRelicType, out Type finalChoraleType)
	{
		if (ProphecyProjectionRelicType != null && FinalChoraleType != null)
		{
			prophecyProjectionRelicType = ProphecyProjectionRelicType;
			finalChoraleType = FinalChoraleType;
			return true;
		}

		Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(static candidate => string.Equals(
				candidate.GetName().Name,
				AssemblyName,
				StringComparison.Ordinal));
		if (assembly == null)
		{
			prophecyProjectionRelicType = null!;
			finalChoraleType = null!;
			return false;
		}

		Type? projectionType = assembly.GetType(ProphecyProjectionRelicTypeName, throwOnError: false);
		Type? choraleType = assembly.GetType(FinalChoraleTypeName, throwOnError: false);
		if (projectionType == null
			|| choraleType == null
			|| !typeof(RelicModel).IsAssignableFrom(projectionType))
		{
			prophecyProjectionRelicType = null!;
			finalChoraleType = null!;
			return false;
		}

		ProphecyProjectionRelicType = projectionType;
		FinalChoraleType = choraleType;
		prophecyProjectionRelicType = projectionType;
		finalChoraleType = choraleType;
		return true;
	}
}

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunesSponsorPack;

// 与「集成战略事件」(IntegratedStrategyEvents,软联动,不引用其程序集)的桥:按类型全名找它的模型。
internal static class IntegratedStrategyEventsBridge
{
	private const string AssemblyName = "IntegratedStrategyEvents";
	private const string ProphecyProjectionRelicTypeName = "IntegratedStrategyEvents.Relics.ProphecyProjectionRelic";
	private const string EndlessKeyRelicTypeName = "IntegratedStrategyEvents.Relics.EndlessKeyRelic";
	private const string FinalChoraleTypeName = "IntegratedStrategyEvents.Encounters.FinalChorale";
	private const int FinalChoraleRandomRelicRewardCount = 2;
	private const string LogTag = "ISE";

	// 解析结果缓存:找到就一直用。
	private static Type? _prophecyProjectionRelicType;
	private static Type? _endlessKeyRelicType;
	private static Type? _finalChoraleType;

	internal static bool IsAvailable => TryResolveTypes(out _, out _);

	// 在 Creature.SetCurrentHpInternal 前缀里高频调用,只比对类型全名,不解析程序集。
	internal static bool IsFinalChorale(Creature creature)
	{
		return creature.Monster?.GetType().FullName == FinalChoraleTypeName;
	}

	internal static async Task<bool> ObtainProphecyProjection(Player owner, int? choraleHp = null)
	{
		if (!TryResolveTypes(out Type? projectionType, out _)
			|| !TryCreateMutableRelic(projectionType, out RelicModel? projection))
		{
			return false;
		}

		if (choraleHp.HasValue)
		{
			ConfigureProjectionChoraleHp(projection, choraleHp.Value);
			await RemoveExistingProphecyProjections(owner, projectionType);
		}

		await RelicCmd.Obtain(projection, owner);
		return true;
	}

	internal static void AddFinalChoraleRewardsIfMissing(CombatRoom room)
	{
		foreach (Player player in room.CombatState.Players)
		{
			if (HasEndlessKeyReward(room, player))
			{
				continue;
			}

			if (!TryResolveEndlessKeyType(out Type? endlessKeyType)
				|| !TryCreateMutableRelic(endlessKeyType, out RelicModel? endlessKey))
			{
				SponsorLog.Warn(LogTag, "Failed to add Final Chorale rewards: EndlessKeyRelic is unavailable.");
				return;
			}

			for (int i = 0; i < FinalChoraleRandomRelicRewardCount; i++)
			{
				room.AddExtraReward(player, new RelicReward(player));
			}

			room.AddExtraReward(player, new RelicReward(endlessKey, player));
			SponsorLog.Info(LogTag, $"Added missing Final Chorale completion rewards for player {player.NetId}.");
		}
	}

	private static bool TryCreateMutableRelic(Type relicType, [NotNullWhen(true)] out RelicModel? relic)
	{
		relic = null;
		try
		{
			if (!ModelDb.Contains(relicType))
			{
				SponsorLog.Warn(LogTag, $"Failed to create {relicType.Name}: model type is not registered in ModelDb.");
				return false;
			}

			relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(relicType)).ToMutable();
			return true;
		}
		catch (Exception ex)
		{
			// 真实边界:第三方模型的构造/ToMutable。
			SponsorLog.Warn(LogTag, $"Failed to create {relicType.Name}: {ex.Message}");
			return false;
		}
	}

	// 按名字写 IntegratedStrategyEvents 0.5.6 ProphecyProjectionRelic 的 private [SavedProperty] 属性
	// (SavedProphecyProjectionChoraleDefeated / ...RewardsGranted / ...UsesScaledChoraleHp / ...ChoraleHp)。
	// 对方改名或删属性时只会部分生效并 Warn,不影响投影遗物本身的发放。
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
			SponsorLog.Warn(LogTag, "ProphecyProjectionRelic HP bonus was only partially configured.");
		}
	}

	private static async Task RemoveExistingProphecyProjections(Player owner, Type projectionType)
	{
		List<RelicModel> existingProjections = [];
		foreach (Player player in owner.RunState.Players)
		{
			foreach (RelicModel relic in player.Relics)
			{
				if (!projectionType.IsInstanceOfType(relic)
					|| relic.IsMelted
					|| relic.HasBeenRemovedFromState)
				{
					continue;
				}

				existingProjections.Add(relic);
			}
		}

		foreach (RelicModel relic in existingProjections)
		{
			await RelicCmd.Remove(relic);
		}

		if (existingProjections.Count > 0)
		{
			SponsorLog.Info(LogTag, $"Removed {existingProjections.Count} old ProphecyProjectionRelic instance(s) before granting the empowered projection.");
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
			SponsorLog.Warn(LogTag, $"Failed to set {propertyName} on ProphecyProjectionRelic: {ex.Message}");
			return false;
		}
	}

	private static bool HasEndlessKeyReward(CombatRoom room, Player player)
	{
		if (!room.ExtraRewards.TryGetValue(player, out List<Reward>? rewards))
		{
			return false;
		}

		return rewards.OfType<RelicReward>().Any(reward => IsEndlessKey(reward.Relic));
	}

	private static bool IsEndlessKey(RelicModel? relic)
	{
		return relic != null
			&& TryResolveEndlessKeyType(out Type? endlessKeyType)
			&& endlessKeyType.IsInstanceOfType(relic);
	}

	private static bool TryResolveTypes(
		[NotNullWhen(true)] out Type? prophecyProjectionRelicType,
		[NotNullWhen(true)] out Type? finalChoraleType)
	{
		if (_prophecyProjectionRelicType != null && _finalChoraleType != null)
		{
			prophecyProjectionRelicType = _prophecyProjectionRelicType;
			finalChoraleType = _finalChoraleType;
			return true;
		}

		prophecyProjectionRelicType = null;
		finalChoraleType = null;
		if (!TryResolveAssembly(out Assembly? assembly))
		{
			return false;
		}

		Type? projectionType = assembly.GetType(ProphecyProjectionRelicTypeName, throwOnError: false);
		Type? choraleType = assembly.GetType(FinalChoraleTypeName, throwOnError: false);
		if (projectionType == null
			|| choraleType == null
			|| !typeof(RelicModel).IsAssignableFrom(projectionType))
		{
			return false;
		}

		_prophecyProjectionRelicType = prophecyProjectionRelicType = projectionType;
		_finalChoraleType = finalChoraleType = choraleType;
		return true;
	}

	private static bool TryResolveEndlessKeyType([NotNullWhen(true)] out Type? endlessKeyRelicType)
	{
		endlessKeyRelicType = _endlessKeyRelicType;
		if (endlessKeyRelicType != null)
		{
			return true;
		}

		if (!TryResolveAssembly(out Assembly? assembly))
		{
			return false;
		}

		Type? relicType = assembly.GetType(EndlessKeyRelicTypeName, throwOnError: false);
		if (relicType == null || !typeof(RelicModel).IsAssignableFrom(relicType))
		{
			return false;
		}

		_endlessKeyRelicType = endlessKeyRelicType = relicType;
		return true;
	}

	private static bool TryResolveAssembly([NotNullWhen(true)] out Assembly? assembly)
	{
		assembly = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(static candidate => string.Equals(
				candidate.GetName().Name,
				AssemblyName,
				StringComparison.Ordinal));
		return assembly != null;
	}
}

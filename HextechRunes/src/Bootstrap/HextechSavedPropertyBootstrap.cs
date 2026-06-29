using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechSavedPropertyBootstrap
{
	internal static void InjectModelType(Type type)
	{
		SavedPropertiesTypeCache.InjectTypeIntoCache(type);
	}

	internal static void InjectCaches()
	{
		foreach (Type type in HextechModelTypeIdentity.Distinct(HextechCatalog.GetAllCustomRelicTypes()))
		{
			SavedPropertiesTypeCache.InjectTypeIntoCache(type);
		}

		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechMayhemModifier));
		foreach (Type type in HextechCustomRunModifierHooks.CustomRarityModifierTypes)
		{
			SavedPropertiesTypeCache.InjectTypeIntoCache(type);
		}

		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechBurnPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryDexterityPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryStrengthLossPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryDexterityLossPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechLethalTempoTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechBloodPactTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechPowerShieldTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechAttackReplayPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechPlayerSlowPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporarySlowPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechOceanDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechInfernalDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechMountainDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechChemtechDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechCloudDragonSoulPower));
		WarnOnUninjectedSavedPropertyCarriers();
		EnsureSavedPropertyNetIdBitSize();
	}

	// R3 启动自检:扫本程序集所有 AbstractModel 载体,凡带 [SavedProperty] 却没进 net-id 表的属性名都告警。
	// 与游戏一致地按【属性名】核对(SavedPropertiesTypeCache 也是按名注册/查询):缺名会让联机(反)序列化
	// 在 GetNetIdForPropertyName 抛 "could not be mapped" → 概率性 1014/ModMismatch。此处只 Log.Warn、绝不抛,
	// 把"漏登记 rune / 漏加 Power 到手写清单"这类隐患从线上崩提前到启动日志。时机:所有 InjectTypeIntoCache 之后、
	// 后续规范化只重排不增删名字,故此刻名字集合已是最终集合。
	private static void WarnOnUninjectedSavedPropertyCarriers()
	{
		try
		{
			HashSet<string>? registeredNames = TryGetRegisteredSavedPropertyNames();
			if (registeredNames == null)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] SavedProperty 注入自检跳过:取不到 net-id 名字表。");
				return;
			}

			System.Type abstractModelType = typeof(MegaCrit.Sts2.Core.Models.AbstractModel);
			HashSet<string> warned = new(StringComparer.Ordinal);
			const BindingFlags propertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			foreach (System.Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (type.IsAbstract || !type.IsClass || !abstractModelType.IsAssignableFrom(type))
				{
					continue;
				}

				foreach (PropertyInfo property in type.GetProperties(propertyFlags))
				{
					bool isSavedProperty = property
						.GetCustomAttributes(inherit: true)
						.Any(static attr => attr.GetType().Name == "SavedPropertyAttribute");
					if (!isSavedProperty || registeredNames.Contains(property.Name) || !warned.Add(property.Name))
					{
						continue;
					}

					Log.Warn($"[{ModInfo.Id}][Mayhem] SavedProperty 注入自检:载体 {type.FullName} 的 [SavedProperty] \"{property.Name}\" 未进 net-id 表;联机(反)序列化会抛 \"could not be mapped\" 致 1014/ModMismatch。请把该类型登记进 catalog,或加入 InjectCaches 的注入清单。");
				}
			}
		}
		catch (System.Exception ex)
		{
			// 纯诊断:任何反射异常都不得影响模组加载。
			Log.Warn($"[{ModInfo.Id}][Mayhem] SavedProperty 注入自检跳过: {ex.Message}");
		}
	}

	private static HashSet<string>? TryGetRegisteredSavedPropertyNames()
	{
		FieldInfo? mapField = TryGetField(
			typeof(SavedPropertiesTypeCache),
			"_netIdToPropertyNameMap",
			BindingFlags.NonPublic | BindingFlags.Static);
		if (mapField?.GetValue(null) is not System.Collections.IEnumerable names)
		{
			return null;
		}

		HashSet<string> result = new(StringComparer.Ordinal);
		foreach (object? name in names)
		{
			if (name is string text)
			{
				result.Add(text);
			}
		}

		return result;
	}

	private static void EnsureSavedPropertyNetIdBitSize()
	{
		// 兜底:按与游戏 / RitsuLib 一致的公式 CeilToInt(Log2(count)) 把位宽抬到能容纳当前属性数。
		// 联机一致性的权威设置由 HextechSavedPropertyNetIdHooks 在规范化后统一完成;此处仅保证即便该
		// 后缀钩子未能安装,本模组单独联机时位宽也够用。不再使用旧的固定下限 16——它与原版 / RitsuLib 的
		// 公式不一致,会让一端是 16、另一端是 CeilToInt(Log2(count)),造成 net-id 位宽错位而断连。
		const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

		FieldInfo? mapField = TryGetField(typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap", flags);
		int propertyNameCount = (mapField?.GetValue(null) as System.Collections.ICollection)?.Count ?? 0;
		int targetBitSize = HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(propertyNameCount);
		int currentBitSize = SavedPropertiesTypeCache.NetIdBitSize;
		if (currentBitSize >= targetBitSize)
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize unchanged: bitSize={currentBitSize} propertyNames={propertyNameCount}");
			return;
		}

		FieldInfo? backingField = TryGetField(typeof(SavedPropertiesTypeCache), "<NetIdBitSize>k__BackingField", flags);
		if (backingField == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize backing field not found; custom saved properties may desync in multiplayer.");
			return;
		}

		backingField.SetValue(null, targetBitSize);
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize updated: old={currentBitSize} new={targetBitSize} propertyNames={propertyNameCount}");
	}
}

using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Relics;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class AssetHooks
{
	private static readonly Dictionary<string, Texture2D> TextureCache = new();
	private static readonly Dictionary<string, CompressedTexture2D> CompressedTextureCache = new();

	private static readonly FieldInfo? NRelicModelField = TryGetField(typeof(NRelic), "_model");

	public static void Install(Harmony harmony)
	{
		MethodInfo getRelicIcon = RequireGetter(typeof(RelicModel), nameof(RelicModel.Icon));
		MethodInfo getRelicIconOutline = RequireGetter(typeof(RelicModel), nameof(RelicModel.IconOutline));
		MethodInfo getRelicBigIcon = RequireGetter(typeof(RelicModel), nameof(RelicModel.BigIcon));
		MethodInfo? relicReload = TryGetMethod(typeof(NRelic), "Reload", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo getPowerIcon = RequireGetter(typeof(PowerModel), nameof(PowerModel.Icon));
		MethodInfo getPowerBigIcon = RequireGetter(typeof(PowerModel), nameof(PowerModel.BigIcon));
		MethodInfo getCardPortrait = RequireGetter(typeof(CardModel), nameof(CardModel.Portrait));
		MethodInfo getEnchantmentIcon = RequireGetter(typeof(EnchantmentModel), nameof(EnchantmentModel.Icon));

		harmony.Patch(getRelicIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicIconPostfix)));
		harmony.Patch(getRelicIconOutline, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicIconOutlinePostfix)));
		harmony.Patch(getRelicBigIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicBigIconPostfix)));
		if (relicReload != null && NRelicModelField != null)
		{
			harmony.Patch(relicReload, prefix: new HarmonyMethod(typeof(AssetHooks), nameof(NRelicReloadPrefix)));
		}
		else
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] NRelic.Reload asset hook skipped: missing {(relicReload == null ? "NRelic.Reload" : "NRelic._model")}.");
		}
		harmony.Patch(getPowerIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(PowerIconPostfix)));
		harmony.Patch(getPowerBigIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(PowerBigIconPostfix)));
		harmony.Patch(getCardPortrait, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(CardPortraitPostfix)));
		harmony.Patch(getEnchantmentIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(EnchantmentIconPostfix)));

		// 自定义休息室选项(目前为「添柴」StokeRestSiteOption)的图标修复。
		// 基类 RestSiteOption.Icon 从 res://images/ui/rest_site/option_<id>.png 取图,模组无法在该 base-game
		// 命名空间提供真实资源,旧实现用可被卸载的缓存别名兜底,在联机非持有方会取到 null 并在渲染思考气泡时抛
		// NotImplementedException —— 该异常发生在同步的 ChooseOption 路径里,导致离开休息室时校验和分叉、客户端被踢。
		// 这里用前缀直接返回稳定纹理(原版 Stoke 卡牌立绘),保证任何端、任何时机 Icon 都有效。
		MethodInfo? getRestSiteOptionIcon = AccessTools.PropertyGetter(typeof(RestSiteOption), nameof(RestSiteOption.Icon));
		if (getRestSiteOptionIcon != null)
		{
			harmony.Patch(getRestSiteOptionIcon, prefix: new HarmonyMethod(typeof(AssetHooks), nameof(RestSiteOptionIconPrefix)));
		}
		else
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] RestSiteOption.Icon asset hook skipped: getter not found (custom rest-site option icons may fail to render and could desync multiplayer).");
		}
	}

	/// <summary>
	/// 为自定义休息室选项提供稳定的图标。仅拦截 <see cref="StokeRestSiteOption"/>:返回原版 Stoke 立绘并跳过原版
	/// 缓存查找(原版查找在该 mod 路径上必然 miss,并可能因 thought bubble 以 null 初始化而抛异常)。
	/// 其它(原版)选项一律放行原逻辑。解析失败时同样放行,退化为修复前行为,绝不抛出。
	/// </summary>
	private static bool RestSiteOptionIconPrefix(RestSiteOption __instance, ref Texture2D __result)
	{
		if (__instance is StokeRestSiteOption && StokeRestSiteOption.ResolveIcon() is { } icon)
		{
			__result = icon;
			return false;
		}

		return true;
	}

	private static void CardPortraitPostfix(CardModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechCardTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static void EnchantmentIconPostfix(EnchantmentModel __instance, ref CompressedTexture2D __result)
	{
		ModelId id = __instance.CanonicalInstance?.Id ?? __instance.Id;
		if (HextechExternalContentRegistry.GetEnchantmentIconPath(id) is { } iconPath
			&& LoadCompressedTexture(iconPath) is { } texture)
		{
			__result = texture;
			return;
		}

		if (__instance is UniversalSpiral)
		{
			__result = ModelDb.Enchantment<Spiral>().Icon;
		}
	}

	private static void RelicIconPostfix(RelicModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechRelicTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static void RelicIconOutlinePostfix(RelicModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechRelicTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static void RelicBigIconPostfix(RelicModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechRelicTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static bool NRelicReloadPrefix(NRelic __instance)
	{
		if (!__instance.IsNodeReady()
			|| NRelicModelField == null
			|| NRelicModelField.GetValue(__instance) is not RelicModel model
			|| !TryGetHextechRelicTexture(model, out Texture2D? texture))
		{
			return true;
		}

		model.UpdateTexture(__instance.Icon);
		__instance.Icon.Texture = texture;
		__instance.Outline.Visible = false;
		return false;
	}

	private static void PowerIconPostfix(PowerModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechPowerTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static void PowerBigIconPostfix(PowerModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechPowerTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static bool TryGetHextechRelicTexture(RelicModel self, out Texture2D? texture)
	{
		texture = null;
		string? path = HextechAssets.TryGetCustomRelicIconPath(self);
		if (path == null)
		{
			return false;
		}

		texture = LoadPortableTexture(path);
		return texture != null;
	}

	private static bool TryGetHextechPowerTexture(PowerModel self, out Texture2D? texture)
	{
		texture = null;
		if (self is HextechPlayerSlowPower)
		{
			texture = ModelDb.Power<SlowPower>().Icon;
			return texture != null;
		}

		string? path = self switch
		{
			HextechBurnPower => $"res://{ModInfo.Id}/images/powers/hextechBurnPower.png",
			HextechAttackReplayPower => $"res://{ModInfo.Id}/images/powers/hextechAttackReplayPower.png",
			HextechOceanDragonSoulPower => HextechAssets.OceanDragonSoulPowerIconPath,
			HextechInfernalDragonSoulPower => HextechAssets.InfernalDragonSoulPowerIconPath,
			HextechDragonSoulPower => HextechAssets.HextechDragonSoulPowerIconPath,
			HextechMountainDragonSoulPower => HextechAssets.MountainDragonSoulPowerIconPath,
			HextechChemtechDragonSoulPower => HextechAssets.ChemtechDragonSoulPowerIconPath,
			HextechCloudDragonSoulPower => HextechAssets.CloudDragonSoulPowerIconPath,
			_ => null
		};
		if (path == null)
		{
			return false;
		}

		texture = LoadPortableTexture(path);
		return texture != null;
	}

	private static bool TryGetHextechCardTexture(CardModel self, out Texture2D? texture)
	{
		texture = null;
		string? path = self switch
		{
			ElicitCard => HextechAssets.ElicitCardPortraitPath,
			TrickMagicCard => HextechAssets.TrickMagicCardPortraitPath,
			BladeWaltzCard => HextechAssets.BladeWaltzCardPortraitPath,
			OceanDragonSoulCard => HextechAssets.OceanDragonSoulCardPortraitPath,
			InfernalDragonSoulCard => HextechAssets.InfernalDragonSoulCardPortraitPath,
			HextechDragonSoulCard => HextechAssets.HextechDragonSoulCardPortraitPath,
			MountainDragonSoulCard => HextechAssets.MountainDragonSoulCardPortraitPath,
			ChemtechDragonSoulCard => HextechAssets.ChemtechDragonSoulCardPortraitPath,
			CloudDragonSoulCard => HextechAssets.CloudDragonSoulCardPortraitPath,
			MikaelsBlessingCard => HextechAssets.MikaelsBlessingCardPortraitPath,
			_ => null
		};
		if (path == null)
		{
			return false;
		}

		texture = LoadPortableTexture(path);
		return texture != null;
	}

	internal static Texture2D? LoadUiTexture(string path)
	{
		return LoadPortableTexture(path);
	}

	private static Texture2D? LoadPortableTexture(string path)
	{
		if (ResourceLoader.Load<Texture2D>(path) is Texture2D loadedTexture)
		{
			TextureCache[path] = loadedTexture;
			return loadedTexture;
		}

		if (TextureCache.TryGetValue(path, out Texture2D? cachedTexture))
		{
			return cachedTexture;
		}

		byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
		if (bytes.Length == 0)
		{
			return null;
		}

		Image image = new();
		Error err = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
			? image.LoadPngFromBuffer(bytes)
			: image.LoadJpgFromBuffer(bytes);
		if (err != Error.Ok)
		{
			return null;
		}

		PortableCompressedTexture2D texture = new();
		texture.CreateFromImage(image, PortableCompressedTexture2D.CompressionMode.Lossless);
		texture.ResourcePath = path;
		TextureCache[path] = texture;
		return texture;
	}

	private static CompressedTexture2D? LoadCompressedTexture(string path)
	{
		if (ResourceLoader.Load<CompressedTexture2D>(path) is CompressedTexture2D loadedTexture)
		{
			CompressedTextureCache[path] = loadedTexture;
			return loadedTexture;
		}

		return CompressedTextureCache.TryGetValue(path, out CompressedTexture2D? cachedTexture)
			? cachedTexture
			: null;
	}

}

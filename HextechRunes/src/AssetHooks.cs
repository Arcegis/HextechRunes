using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace HextechRunes;

internal static class AssetHooks
{
	private static readonly Dictionary<string, Texture2D> TextureCache = new();

	private static readonly FieldInfo NRelicModelField = typeof(NRelic).GetField("_model", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("Could not access NRelic._model.");

	public static void Install(Harmony harmony)
	{
		MethodInfo getRelicIcon = RequireGetter(typeof(RelicModel), nameof(RelicModel.Icon));
		MethodInfo getRelicBigIcon = RequireGetter(typeof(RelicModel), nameof(RelicModel.BigIcon));
		MethodInfo relicReload = RequireMethod(typeof(NRelic), "Reload", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo getPowerIcon = RequireGetter(typeof(PowerModel), nameof(PowerModel.Icon));
		MethodInfo getPowerBigIcon = RequireGetter(typeof(PowerModel), nameof(PowerModel.BigIcon));
		MethodInfo getCardPortrait = RequireGetter(typeof(CardModel), nameof(CardModel.Portrait));

		harmony.Patch(getRelicIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicIconPostfix)));
		harmony.Patch(getRelicBigIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(RelicBigIconPostfix)));
		harmony.Patch(relicReload, prefix: new HarmonyMethod(typeof(AssetHooks), nameof(NRelicReloadPrefix)));
		harmony.Patch(getPowerIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(PowerIconPostfix)));
		harmony.Patch(getPowerBigIcon, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(PowerBigIconPostfix)));
		harmony.Patch(getCardPortrait, postfix: new HarmonyMethod(typeof(AssetHooks), nameof(CardPortraitPostfix)));
	}

	private static void CardPortraitPostfix(CardModel __instance, ref Texture2D __result)
	{
		if (TryGetHextechCardTexture(__instance, out Texture2D? texture))
		{
			__result = texture!;
		}
	}

	private static void RelicIconPostfix(RelicModel __instance, ref Texture2D __result)
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
		string? path = ModInfo.TryGetCustomRelicIconPath(self);
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
		string? path = self switch
		{
			HextechBurnPower => $"res://{ModInfo.Id}/images/powers/hextechBurnPower.png",
			HextechAttackReplayPower => $"res://{ModInfo.Id}/images/powers/hextechAttackReplayPower.png",
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
			ElicitCard => ModInfo.ElicitCardPortraitPath,
			TrickMagicCard => ModInfo.TrickMagicCardPortraitPath,
			BladeWaltzCard => ModInfo.BladeWaltzCardPortraitPath,
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

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameterTypes)
	{
		return type.GetMethod(name, flags, binder: null, parameterTypes, modifiers: null)
			?? throw new InvalidOperationException($"Could not find method {type.FullName}.{name}.");
	}

	private static MethodInfo RequireGetter(Type type, string propertyName)
	{
		return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetMethod
			?? throw new InvalidOperationException($"Could not find property getter {type.FullName}.{propertyName}.");
	}
}

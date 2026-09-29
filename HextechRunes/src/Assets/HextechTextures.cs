using Godot;

namespace HextechRunes;

/// <summary>
/// 模组自建 UI 与特效用的纹理加载:原始 PNG/JPG 手动解码进模组私有缓存,不设 ResourcePath、不进原版 AssetCache
/// （原版 Cache 会 Dispose 路径相同但类型不符的资源，不能将自建纹理挂到其路径上）。
/// 走原版模型 getter 的图标(遗物/卡牌/能力)不经这里,直接由 PCK 内已导入资源满足。
/// </summary>
internal static class HextechTextures
{
	private static readonly Dictionary<string, Texture2D> TextureCache = new();
	private static readonly Dictionary<string, CompressedTexture2D> CompressedTextureCache = new();
	private static readonly Dictionary<string, Texture2D?> VanillaTextureCache = new(StringComparer.Ordinal);
	private static readonly HashSet<string> WarnedTextureMissPaths = new(StringComparer.Ordinal);
	private static Texture2D? _missingTexture;

	/// <summary>加载本模组自带的 UI/特效纹理;未命中时每个路径只告警一次,调用方不必再自行告警。</summary>
	internal static Texture2D? LoadUiTexture(string path)
	{
		// 原始图像字节在支持的游戏版本间稳定,图片资源只手动解码;非图片路径(.tres 等)才走 ResourceLoader。
		Func<string, Texture2D?> secondaryLoader = AssetResourceResolver.IsRawImagePath(path)
			? static _ => null
			: LoadTextureThroughResourceLoader;
		Texture2D? texture = AssetResourceResolver.Resolve(
			path,
			TextureCache,
			IsTextureUsable,
			LoadRawImageTexture,
			secondaryLoader);
		if (texture == null)
		{
			WarnTextureMissOnce(
				path,
				AssetResourceResolver.IsRawImagePath(path)
					? "raw image decode miss"
					: "raw decode and ResourceLoader miss");
		}

		return texture;
	}

	internal static CompressedTexture2D? LoadCompressedTexture(string path)
	{
		if (CompressedTextureCache.TryGetValue(path, out CompressedTexture2D? cachedTexture))
		{
			if (IsTextureUsable(cachedTexture))
			{
				return cachedTexture;
			}

			CompressedTextureCache.Remove(path);
		}

		try
		{
			if (ResourceLoader.Load<CompressedTexture2D>(path) is { } loadedTexture && IsTextureUsable(loadedTexture))
			{
				CompressedTextureCache[path] = loadedTexture;
				return loadedTexture;
			}
		}
		catch (Exception ex)
		{
			LogFailure(nameof(LoadCompressedTexture), ex);
		}

		WarnTextureMissOnce(path, "ResourceLoader returned no usable compressed texture");
		return null;
	}

	/// <summary>
	/// 加载原版 PCK 内的贴图(已导入资源,走 ResourceLoader);失败返回 null,调用方回退到程序化纹理。
	/// 结果(含未命中)按路径缓存,避免每次建粒子都重新查找。
	/// </summary>
	internal static Texture2D? LoadVanillaTexture(string resPath)
	{
		if (VanillaTextureCache.TryGetValue(resPath, out Texture2D? cached))
		{
			return cached != null && GodotObject.IsInstanceValid(cached) ? cached : null;
		}

		Texture2D? texture = ResourceLoader.Load(resPath) as Texture2D;
		VanillaTextureCache[resPath] = texture;
		return texture;
	}

	internal static Texture2D? GetMissingTexture()
	{
		if (IsTextureUsable(_missingTexture))
		{
			return _missingTexture;
		}

		try
		{
			const int size = 64;
			Image image = Image.CreateEmpty(size, size, useMipmaps: false, Image.Format.Rgba8);
			image.Fill(new Color(0.12f, 0.14f, 0.18f, 0.96f));
			Color accent = new(0.72f, 0.76f, 0.84f, 0.9f);
			for (int i = 10; i < size - 10; i++)
			{
				image.SetPixel(i, i, accent);
				image.SetPixel(size - 1 - i, i, accent);
			}

			_missingTexture = ImageTexture.CreateFromImage(image);
			return IsTextureUsable(_missingTexture) ? _missingTexture : null;
		}
		catch (Exception ex)
		{
			LogFailure(nameof(GetMissingTexture), ex);
			return null;
		}
	}

	internal static bool IsTextureUsable(Texture2D? texture)
	{
		if (texture == null)
		{
			return false;
		}

		try
		{
			return GodotObject.IsInstanceValid(texture) && texture.GetWidth() > 0 && texture.GetHeight() > 0;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	internal static void LogFailure(string site, Exception ex)
	{
		if (HextechRunLogBudget.TryConsume("assets.hook-failure", 10))
		{
			HextechLog.Error("Assets", $"{site} failed; keeping original result: {ex}");
		}
	}

	private static Texture2D? LoadRawImageTexture(string path)
	{
		return TryLoadRawImageTexture(path, out Texture2D? texture) ? texture : null;
	}

	private static Texture2D? LoadTextureThroughResourceLoader(string path)
	{
		try
		{
			return ResourceLoader.Load<Texture2D>(path);
		}
		catch (Exception ex)
		{
			LogFailure(nameof(LoadTextureThroughResourceLoader), ex);
			return null;
		}
	}

	private static bool TryLoadRawImageTexture(string path, out Texture2D? texture)
	{
		texture = null;
		if (!AssetResourceResolver.IsRawImagePath(path))
		{
			return false;
		}

		bool isPng = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

		// 解码与建纹理整体兜底:Resolve 按契约不 catch,LoadUiTexture 链上也没有别的护栏。
		try
		{
			byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
			if (bytes.Length == 0)
			{
				return false;
			}

			Image image = new();
			Error err = isPng
				? image.LoadPngFromBuffer(bytes)
				: image.LoadJpgFromBuffer(bytes);
			if (err != Error.Ok)
			{
				return false;
			}

			ImageTexture? imageTexture = ImageTexture.CreateFromImage(image);
			if (!IsTextureUsable(imageTexture))
			{
				imageTexture?.Dispose();
				return false;
			}

			texture = imageTexture;
			return true;
		}
		catch (Exception ex)
		{
			LogFailure(nameof(TryLoadRawImageTexture), ex);
			return false;
		}
	}

	// 加载失败最终表现是静默 NOPE 占位,肉眼难归因;每路径告警一次方便定位打包/命名问题。
	private static void WarnTextureMissOnce(string path, string reason)
	{
		if (WarnedTextureMissPaths.Add(path))
		{
			HextechLog.Warn("Assets", $"Texture load miss ({reason}): {path}");
		}
	}
}

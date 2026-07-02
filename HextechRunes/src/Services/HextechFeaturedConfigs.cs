using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

/// <summary>
/// 社区精选配置：从官方服务器拉取人工审核后的配置列表（静态 JSON，只读），
/// 玩家在配置菜单里浏览并一键应用（应用走与"导入配置码"相同的 pending 填充流程）。
/// </summary>
internal static class HextechFeaturedConfigs
{
	private const int MaxEntries = 100;
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
	private static readonly System.Net.Http.HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(12)
	};

	private static readonly object CacheLock = new();
	private static IReadOnlyList<FeaturedConfigEntry>? _cached;
	private static DateTime _cachedAtUtc;

	internal sealed record FeaturedConfigEntry(
		[property: JsonPropertyName("id")] string? Id,
		[property: JsonPropertyName("name")] string? Name,
		[property: JsonPropertyName("author")] string? Author,
		[property: JsonPropertyName("description")] string? Description,
		[property: JsonPropertyName("code")] string? Code);

	private sealed record FeaturedConfigsDocument(
		[property: JsonPropertyName("schemaVersion")] int SchemaVersion,
		[property: JsonPropertyName("configs")] List<FeaturedConfigEntry>? Configs);

	internal sealed record CommunityConfigEntry(
		[property: JsonPropertyName("id")] string? Id,
		[property: JsonPropertyName("title")] string? Title,
		[property: JsonPropertyName("author")] string? Author,
		[property: JsonPropertyName("code")] string? Code,
		[property: JsonPropertyName("likes")] int Likes,
		[property: JsonPropertyName("createdAt")] string? CreatedAt,
		[property: JsonPropertyName("hidden")] bool Hidden = false);

	private sealed record CommunityListDocument(
		[property: JsonPropertyName("schemaVersion")] int SchemaVersion,
		[property: JsonPropertyName("configs")] List<CommunityConfigEntry>? Configs);

	internal sealed record CommunityApiResult(bool Ok, string? Error, string? Id, int Likes);

	/// <summary>拉取精选配置列表；失败返回 null（调用方展示错误行）。结果缓存 5 分钟。</summary>
	public static async Task<IReadOnlyList<FeaturedConfigEntry>?> FetchAsync()
	{
		lock (CacheLock)
		{
			if (_cached != null && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
			{
				return _cached;
			}
		}

		try
		{
			using System.Net.Http.HttpResponseMessage response =
				await HttpClient.GetAsync(HextechServerEndpoints.FeaturedConfigsEndpoint).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				Log.Warn($"[{ModInfo.Id}][FeaturedConfigs] HTTP {(int)response.StatusCode}");
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			FeaturedConfigsDocument? document = JsonSerializer.Deserialize<FeaturedConfigsDocument>(json);
			if (document?.Configs == null || document.SchemaVersion != 1)
			{
				Log.Warn($"[{ModInfo.Id}][FeaturedConfigs] unexpected schema");
				return null;
			}

			// 只保留"名字 + 可解析配置码"齐全的条目;解析失败的条目静默丢弃(服务器内容有误不应打搅玩家)。
			List<FeaturedConfigEntry> entries = document.Configs
				.Where(static entry => !string.IsNullOrWhiteSpace(entry.Name)
					&& !string.IsNullOrWhiteSpace(entry.Code)
					&& HextechConfigShareCodec.TryParse(entry.Code) != null)
				.Take(MaxEntries)
				.ToList();

			lock (CacheLock)
			{
				_cached = entries;
				_cachedAtUtc = DateTime.UtcNow;
			}

			return entries;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][FeaturedConfigs] fetch failed: {ex.Message}");
			return null;
		}
	}

	// —— 社区开放列表(热门/最新,服务器物化的静态 json,读路径不打接口)与互动 API ——

	/// <summary>拉社区列表。sort="hot"|"new"。失败返回 null。不缓存(点赞后需要看到新数)。</summary>
	public static async Task<IReadOnlyList<CommunityConfigEntry>?> FetchCommunityAsync(string sort)
	{
		try
		{
			string endpoint = sort == "hot"
				? HextechServerEndpoints.CommunityHotEndpoint
				: HextechServerEndpoints.CommunityNewEndpoint;
			using System.Net.Http.HttpResponseMessage response = await HttpClient.GetAsync(endpoint).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			CommunityListDocument? document = JsonSerializer.Deserialize<CommunityListDocument>(json);
			if (document?.Configs == null || document.SchemaVersion != 1)
			{
				return null;
			}

			return document.Configs
				.Where(static entry => !string.IsNullOrWhiteSpace(entry.Title)
					&& !string.IsNullOrWhiteSpace(entry.Code)
					&& !string.IsNullOrWhiteSpace(entry.Id)
					&& HextechConfigShareCodec.TryParse(entry.Code) != null)
				.ToList();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Community] list fetch failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>「我的」列表：该 SteamId 的全部上传（含被隐藏的，便于玩家知情）。失败返回 null。</summary>
	public static async Task<IReadOnlyList<CommunityConfigEntry>?> FetchMineAsync(string steamId)
	{
		try
		{
			using System.Net.Http.StringContent content = new(
				JsonSerializer.Serialize(new Dictionary<string, string> { ["steamId"] = steamId }),
				System.Text.Encoding.UTF8,
				"application/json");
			using System.Net.Http.HttpResponseMessage response = await HttpClient
				.PostAsync(HextechServerEndpoints.CommunityApiBase + "mine", content).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			CommunityListDocument? document = JsonSerializer.Deserialize<CommunityListDocument>(json);
			return document?.Configs?.Where(static entry => !string.IsNullOrWhiteSpace(entry.Id)).ToList();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Community] mine fetch failed: {ex.Message}");
			return null;
		}
	}

	public static Task<CommunityApiResult> UploadAsync(string steamId, string authorName, string title, string code)
	{
		return PostAsync("upload", new Dictionary<string, object?>
		{
			["steamId"] = steamId,
			["authorName"] = authorName,
			["title"] = title,
			["code"] = code
		});
	}

	public static Task<CommunityApiResult> DeleteAsync(string steamId, string id)
	{
		return PostAsync("delete", new Dictionary<string, object?> { ["steamId"] = steamId, ["id"] = id });
	}

	public static Task<CommunityApiResult> LikeAsync(string steamId, string id, bool on)
	{
		return PostAsync("like", new Dictionary<string, object?> { ["steamId"] = steamId, ["id"] = id, ["on"] = on });
	}

	public static Task<CommunityApiResult> ReportAsync(string steamId, string id)
	{
		return PostAsync("report", new Dictionary<string, object?> { ["steamId"] = steamId, ["id"] = id });
	}

	private static async Task<CommunityApiResult> PostAsync(string action, Dictionary<string, object?> payload)
	{
		try
		{
			using System.Net.Http.StringContent content = new(
				JsonSerializer.Serialize(payload),
				System.Text.Encoding.UTF8,
				"application/json");
			using System.Net.Http.HttpResponseMessage response = await HttpClient
				.PostAsync(HextechServerEndpoints.CommunityApiBase + action, content).ConfigureAwait(false);
			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			using JsonDocument document = JsonDocument.Parse(json);
			JsonElement root = document.RootElement;
			bool ok = root.TryGetProperty("ok", out JsonElement okEl) && okEl.GetBoolean();
			string? error = root.TryGetProperty("error", out JsonElement errEl) ? errEl.GetString() : null;
			string? id = root.TryGetProperty("id", out JsonElement idEl) ? idEl.GetString() : null;
			int likes = root.TryGetProperty("likes", out JsonElement likesEl) && likesEl.TryGetInt32(out int value) ? value : -1;
			return new CommunityApiResult(ok, error, id, likes);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Community] {action} failed: {ex.Message}");
			return new CommunityApiResult(false, "network", null, -1);
		}
	}
}

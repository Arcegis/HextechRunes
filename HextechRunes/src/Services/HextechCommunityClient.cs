using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HextechRunes;

/// <summary>
/// 社区配置的开放列表(热门/最新/我的)与互动 API(上传、删除、点赞、举报)。
/// 列表读路径是服务器物化的静态 JSON;互动接口按 steamId 标识玩家。与只读的精选列表
/// (<see cref="HextechFeaturedConfigs"/>)分开维护。
/// </summary>
internal static class HextechCommunityClient
{
	private const int ListSchemaVersion = 1;

	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(12)
	};

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

	/// <summary>拉社区列表。sort="hot"|"new"。失败返回 null。不缓存(点赞后需要看到新数)。</summary>
	public static async Task<IReadOnlyList<CommunityConfigEntry>?> FetchCommunityAsync(string sort)
	{
		try
		{
			string endpoint = sort == "hot"
				? HextechServerEndpoints.CommunityHotEndpoint
				: HextechServerEndpoints.CommunityNewEndpoint;
			using HttpResponseMessage response = await HttpClient.GetAsync(endpoint).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			CommunityListDocument? document = JsonSerializer.Deserialize<CommunityListDocument>(json);
			if (document?.Configs == null || document.SchemaVersion != ListSchemaVersion)
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
			HextechLog.Warn("Community", $"list fetch failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>「我的」列表：该 SteamId 的全部上传（含被隐藏的，便于玩家知情）。失败返回 null。</summary>
	public static async Task<IReadOnlyList<CommunityConfigEntry>?> FetchMineAsync(string steamId)
	{
		try
		{
			using StringContent content = new(
				JsonSerializer.Serialize(new Dictionary<string, string> { ["steamId"] = steamId }),
				Encoding.UTF8,
				"application/json");
			using HttpResponseMessage response = await HttpClient
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
			HextechLog.Warn("Community", $"mine fetch failed: {ex.Message}");
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
			using StringContent content = new(
				JsonSerializer.Serialize(payload),
				Encoding.UTF8,
				"application/json");
			using HttpResponseMessage response = await HttpClient
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
			HextechLog.Warn("Community", $"{action} failed: {ex.Message}");
			return new CommunityApiResult(false, "network", null, -1);
		}
	}
}

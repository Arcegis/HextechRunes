using System.Text.Json;
using System.Text.Json.Serialization;

namespace HextechRunes;

/// <summary>
/// 社区精选配置：从官方服务器拉取人工审核后的配置列表（静态 JSON，只读），
/// 玩家在配置菜单里浏览并一键应用（应用走与"导入配置码"相同的 pending 填充流程）。
/// </summary>
internal static class HextechFeaturedConfigs
{
	private const int MaxEntries = 100;
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
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
			using HttpResponseMessage response =
				await HextechHttp.GetAsync(HextechServerEndpoints.FeaturedConfigsEndpoint).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				HextechLog.Warn("FeaturedConfigs", $"HTTP {(int)response.StatusCode}");
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			FeaturedConfigsDocument? document = JsonSerializer.Deserialize<FeaturedConfigsDocument>(json);
			if (document?.Configs == null || document.SchemaVersion != 1)
			{
				HextechLog.Warn("FeaturedConfigs", "unexpected schema");
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
			HextechLog.Warn("FeaturedConfigs", $"fetch failed: {ex.Message}");
			return null;
		}
	}
}

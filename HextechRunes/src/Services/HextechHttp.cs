namespace HextechRunes;

/// <summary>
/// 本模组所有 HTTP 请求(更新检查、精选/社区配置、遥测上传)共用的一个 <see cref="HttpClient"/>。
/// 各调用方的超时不同,所以客户端本身不设超时,每次请求用 CancellationToken 限时。
/// 默认 HttpCompletionOption.ResponseContentRead 会在返回前读完响应体,限时覆盖到正文下载。
/// </summary>
internal static class HextechHttp
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(12);

	private static readonly HttpClient Client = new()
	{
		Timeout = Timeout.InfiniteTimeSpan
	};

	internal static async Task<HttpResponseMessage> GetAsync(string url)
	{
		using CancellationTokenSource timeout = new(DefaultTimeout);
		return await Client.GetAsync(url, timeout.Token).ConfigureAwait(false);
	}

	internal static Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
	{
		return PostAsync(url, content, DefaultTimeout);
	}

	internal static async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, TimeSpan timeout)
	{
		using CancellationTokenSource cancellation = new(timeout);
		return await Client.PostAsync(url, content, cancellation.Token).ConfigureAwait(false);
	}
}

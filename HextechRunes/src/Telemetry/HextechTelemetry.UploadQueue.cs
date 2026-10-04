using System.Text;

namespace HextechRunes;

internal static partial class HextechTelemetry
{
	private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(5);

	// 一次上传事务 = 读待发队列 → 逐条 POST(最长约 MaxPendingLines × 超时) → 回写未发出的条目。
	// 两局相继结束时若并发执行,会重复上传同一批条目或互相覆盖队列文件丢条目,所以整段串行化。
	private static readonly SemaphoreSlim UploadGate = new(1, 1);

	private static async Task UploadSerializedAsync(string endpoint, string currentJson, string runId)
	{
		await UploadGate.WaitAsync().ConfigureAwait(false);
		try
		{
			await UploadPendingThenCurrentAsync(endpoint, currentJson, runId).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// 后台任务无人 await:队列文件 IO 异常必须在这里记下,否则只会成为未观察的任务异常。
			HextechLog.Warn("Mayhem", $"Telemetry upload failed run={runId}: {ex.Message}");
		}
		finally
		{
			UploadGate.Release();
		}
	}

	private static async Task UploadPendingThenCurrentAsync(string endpoint, string currentJson, string runId)
	{
		List<string> pending = ReadPendingPayloads();
		pending.Add(currentJson);

		List<string> unsent = [];
		foreach (string payload in pending.TakeLast(MaxPendingLines))
		{
			try
			{
				using StringContent content = new(payload, Encoding.UTF8, "application/json");
				using HttpResponseMessage response = await HextechHttp.PostAsync(endpoint, content, UploadTimeout).ConfigureAwait(false);
				if (!response.IsSuccessStatusCode)
				{
					unsent.Add(payload);
				}
			}
			catch
			{
				unsent.Add(payload);
			}
		}

		WritePendingPayloads(unsent.TakeLast(MaxPendingLines).ToList());
		if (unsent.Count == 0)
		{
			HextechLog.Info("Mayhem", $"Telemetry uploaded run={runId}");
		}
		else
		{
			HextechLog.Warn("Mayhem", $"Telemetry upload deferred unsent={unsent.Count}");
		}
	}

	private static List<string> ReadPendingPayloads()
	{
		string path = GetPendingPath();
		if (!File.Exists(path))
		{
			return [];
		}

		return File.ReadLines(path)
			.Where(static line => !string.IsNullOrWhiteSpace(line))
			.TakeLast(MaxPendingLines)
			.ToList();
	}

	private static void WritePendingPayloads(IReadOnlyList<string> payloads)
	{
		string path = GetPendingPath();
		Directory.CreateDirectory(HextechDataPaths.GetDataDirectory());
		if (payloads.Count == 0)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			return;
		}

		File.WriteAllLines(path, payloads);
	}
}

using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechUpdateChecker
{
	private const string NoticeName = "HextechRunesUpdateNotice";
	private const string StaticVersionEndpoint = "http://39.96.216.77/latest-version.json";
	private const string ApiVersionEndpoint = "http://39.96.216.77/api/hextech-runes/latest-version";
	private const int MaxCheckAttempts = 2;

	private static readonly System.Net.Http.HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(12)
	};

	private static readonly string[] VersionEndpoints = [StaticVersionEndpoint, ApiVersionEndpoint];

	private static readonly object StateLock = new();
	private static Task<UpdateCheckResult>? _checkTask;
	private static UpdateCheckResult? _cachedResult;

	private sealed record UpdateCheckResult(string Text, bool Cacheable);

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NMainMenu), nameof(NMainMenu._Ready), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechUpdateChecker), nameof(MainMenuReadyPostfix)));
	}

	private static void MainMenuReadyPostfix(NMainMenu __instance)
	{
		try
		{
			ShowNotice(__instance);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Update checker UI failed: {ex.Message}");
		}
	}

	private static void ShowNotice(NMainMenu mainMenu)
	{
		RemoveExistingNotice(mainMenu);
		Label label = CreateNotice(mainMenu);
		UpdateCheckResult? cachedResult;
		Task<UpdateCheckResult> checkTask;
		lock (StateLock)
		{
			cachedResult = _cachedResult;
			if (cachedResult != null)
			{
				label.Text = cachedResult.Text;
				return;
			}

			_checkTask ??= CheckLatestVersionAsync();
			checkTask = _checkTask;
		}

		_ = ApplyCheckResultAsync(label, checkTask);
	}

	private static Label CreateNotice(NMainMenu mainMenu)
	{
		Label? template = FindVanillaModStatusLabel(mainMenu);
		Label label = CreateNoticeLabel(template);
		ConfigureNoticePlacement(label);
		SetNoticeText(label, "正在检查海克斯大乱斗模组版本");
		mainMenu.AddChild(label);
		mainMenu.MoveChild(label, mainMenu.GetChildCount() - 1);
		return label;
	}

	private static Label CreateNoticeLabel(Label? template)
	{
		Label label = template is MegaLabel ? new MegaLabel() : new Label();
		label.Name = NoticeName;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.ZIndex = template != null ? Math.Max(template.ZIndex + 1, 200) : 200;
		if (template != null)
		{
			ApplyNoticeStyleFromTemplate(label, template);
			return label;
		}

		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", new Color(0.86f, 0.69f, 0.18f, 0.94f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.58f));
		label.AddThemeConstantOverride("outline_size", 2);
		return label;
	}

	private static void ApplyNoticeStyleFromTemplate(Label label, Label template)
	{
		label.ThemeTypeVariation = template.ThemeTypeVariation;
		Font font = template.GetThemeFont("font");
		if (font != null)
		{
			label.AddThemeFontOverride("font", font);
		}

		int fontSize = template.GetThemeFontSize("font_size");
		if (fontSize > 0)
		{
			label.AddThemeFontSizeOverride("font_size", fontSize);
		}

		label.AddThemeColorOverride("font_color", template.GetThemeColor("font_color"));
		label.AddThemeColorOverride("font_outline_color", template.GetThemeColor("font_outline_color"));
		label.AddThemeColorOverride("font_shadow_color", template.GetThemeColor("font_shadow_color"));
		label.AddThemeConstantOverride("outline_size", template.GetThemeConstant("outline_size"));
		label.AddThemeConstantOverride("shadow_offset_x", template.GetThemeConstant("shadow_offset_x"));
		label.AddThemeConstantOverride("shadow_offset_y", template.GetThemeConstant("shadow_offset_y"));
	}

	private static void ConfigureNoticePlacement(Label label)
	{
		label.HorizontalAlignment = HorizontalAlignment.Left;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AnchorLeft = 0f;
		label.AnchorTop = 1f;
		label.AnchorRight = 0f;
		label.AnchorBottom = 1f;
		label.OffsetLeft = 16f;
		label.OffsetTop = -44f;
		label.OffsetRight = 760f;
		label.OffsetBottom = -14f;
	}

	private static Label? FindVanillaModStatusLabel(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child.Name == NoticeName)
			{
				continue;
			}

			if (child is Label label && IsVanillaModStatusText(label.Text))
			{
				return label;
			}

			Label? nested = FindVanillaModStatusLabel(child);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	private static bool IsVanillaModStatusText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		string normalized = text.Trim();
		if (normalized.Contains("模组", StringComparison.Ordinal) && normalized.Contains("已加载", StringComparison.Ordinal))
		{
			return true;
		}

		return normalized.Contains("mod", StringComparison.OrdinalIgnoreCase)
			&& normalized.Contains("loaded", StringComparison.OrdinalIgnoreCase);
	}

	private static void SetNoticeText(Label label, string text)
	{
		if (label is MegaLabel megaLabel)
		{
			megaLabel.SetTextAutoSize(text);
			return;
		}

		label.Text = text;
	}

	private static void RemoveExistingNotice(Node mainMenu)
	{
		foreach (Node child in mainMenu.GetChildren())
		{
			if (child.Name == NoticeName)
			{
				mainMenu.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	private static async Task ApplyCheckResultAsync(Label label, Task<UpdateCheckResult> checkTask)
	{
		UpdateCheckResult result = await checkTask.ConfigureAwait(false);
		if (!result.Cacheable)
		{
			lock (StateLock)
			{
				if (ReferenceEquals(_checkTask, checkTask))
				{
					_checkTask = null;
				}
			}
		}

		if (GodotObject.IsInstanceValid(label))
		{
			if (label is MegaLabel)
			{
				label.CallDeferred(nameof(MegaLabel.SetTextAutoSize), result.Text);
			}
			else
			{
				label.CallDeferred("set", "text", result.Text);
			}
		}
	}

	private static async Task<UpdateCheckResult> CheckLatestVersionAsync()
	{
		List<string> failures = [];
		for (int attempt = 1; attempt <= MaxCheckAttempts; attempt++)
		{
			foreach (string endpoint in VersionEndpoints)
			{
				UpdateCheckResult? result = await TryCheckEndpointAsync(endpoint, failures).ConfigureAwait(false);
				if (result != null)
				{
					return CacheResult(result);
				}
			}

			if (attempt < MaxCheckAttempts)
			{
				await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
			}
		}

		Log.Warn($"[{ModInfo.Id}][Mayhem] Update check failed: {string.Join("; ", failures)}");
		return new UpdateCheckResult("海克斯大乱斗模组更新检查暂不可用", false);
	}

	private static async Task<UpdateCheckResult?> TryCheckEndpointAsync(string endpoint, List<string> failures)
	{
		try
		{
			using HttpResponseMessage response = await HttpClient.GetAsync(endpoint).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				failures.Add($"{endpoint}: HTTP {(int)response.StatusCode}");
				return null;
			}

			string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			string? latestVersion = TryReadLatestVersion(json);
			if (string.IsNullOrWhiteSpace(latestVersion))
			{
				failures.Add($"{endpoint}: missing latestVersion");
				return null;
			}

			return BuildVersionResult(latestVersion);
		}
		catch (Exception ex)
		{
			failures.Add($"{endpoint}: {ex.Message}");
			return null;
		}
	}

	private static UpdateCheckResult BuildVersionResult(string latestVersion)
	{
		string normalizedLatest = latestVersion.Trim();
		string currentVersion = ModInfo.Version;
		string text = CompareVersions(normalizedLatest, currentVersion) > 0
			? $"海克斯大乱斗模组有新版{normalizedLatest}，当前版本为{currentVersion}"
			: $"海克斯大乱斗模组为最新版{currentVersion}";
		Log.Info($"[{ModInfo.Id}][Mayhem] Update check succeeded: latest={normalizedLatest}, current={currentVersion}");
		return new UpdateCheckResult(text, true);
	}

	private static UpdateCheckResult CacheResult(UpdateCheckResult result)
	{
		lock (StateLock)
		{
			_cachedResult = result;
			return result;
		}
	}

	private static string? TryReadLatestVersion(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;
		return TryGetString(root, "latestVersion")
			?? TryGetString(root, "version")
			?? TryGetString(root, "latest");
	}

	private static string? TryGetString(JsonElement element, string propertyName)
	{
		return element.ValueKind == JsonValueKind.Object
			&& element.TryGetProperty(propertyName, out JsonElement property)
			&& property.ValueKind == JsonValueKind.String
				? property.GetString()
				: null;
	}

	private static int CompareVersions(string left, string right)
	{
		int[] leftParts = ParseVersionParts(left);
		int[] rightParts = ParseVersionParts(right);
		int count = Math.Max(leftParts.Length, rightParts.Length);
		for (int i = 0; i < count; i++)
		{
			int leftPart = i < leftParts.Length ? leftParts[i] : 0;
			int rightPart = i < rightParts.Length ? rightParts[i] : 0;
			int comparison = leftPart.CompareTo(rightPart);
			if (comparison != 0)
			{
				return comparison;
			}
		}

		return 0;
	}

	private static int[] ParseVersionParts(string version)
	{
		string trimmed = version.Trim().TrimStart('v', 'V');
		List<int> parts = [];
		foreach (string segment in trimmed.Split(['.', '-', '+'], StringSplitOptions.RemoveEmptyEntries))
		{
			int value = 0;
			bool hasDigit = false;
			foreach (char c in segment)
			{
				if (!char.IsDigit(c))
				{
					break;
				}

				hasDigit = true;
				value = checked((value * 10) + (c - '0'));
			}

			if (!hasDigit)
			{
				break;
			}

			parts.Add(value);
		}

		return parts.Count == 0 ? [0] : parts.ToArray();
	}

}

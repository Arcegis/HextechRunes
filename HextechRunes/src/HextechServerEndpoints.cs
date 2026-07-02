namespace HextechRunes;

internal static class HextechServerEndpoints
{
	private const string OfficialServerHost = "39.96.216.77";
	private const string OfficialServerBase = "http://" + OfficialServerHost;

	public const string StaticVersionEndpoint = OfficialServerBase + "/latest-version.json";
	public const string ApiVersionEndpoint = OfficialServerBase + "/api/hextech-runes/latest-version";
	public const string TelemetryEndpoint = OfficialServerBase + "/api/hextech-runes/run-result";
	public const string FeaturedConfigsEndpoint = OfficialServerBase + "/featured-configs.json";
	public const string CommunityHotEndpoint = OfficialServerBase + "/community-hot.json";
	public const string CommunityNewEndpoint = OfficialServerBase + "/community-new.json";
	public const string CommunityApiBase = OfficialServerBase + "/api/hextech-runes/community/";

	public static bool IsOfficialEndpoint(string endpoint)
	{
		if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
		{
			return false;
		}

		return string.Equals(uri.Host, OfficialServerHost, StringComparison.OrdinalIgnoreCase)
			&& (string.Equals(uri.AbsolutePath, "/latest-version.json", StringComparison.Ordinal)
				|| uri.AbsolutePath.StartsWith("/api/hextech-runes/", StringComparison.Ordinal));
	}
}

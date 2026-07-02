using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace HextechRunes;

/// <summary>社区配置的身份来源：SteamId64 + Steam 昵称。Steam 未初始化时社区互动功能不可用。</summary>
internal static class HextechSteamIdentity
{
	public static bool TryGetSteamId(out string steamId)
	{
		steamId = string.Empty;
		try
		{
			if (!SteamInitializer.Initialized)
			{
				return false;
			}

			ulong id = SteamUser.GetSteamID().m_SteamID;
			if (id == 0)
			{
				return false;
			}

			steamId = id.ToString();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static string GetPersonaName()
	{
		try
		{
			return SteamInitializer.Initialized ? SteamFriends.GetPersonaName() : "Player";
		}
		catch
		{
			return "Player";
		}
	}
}

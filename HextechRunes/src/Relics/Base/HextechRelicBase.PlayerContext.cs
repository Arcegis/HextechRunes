using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

public abstract partial class HextechRelicBase
{
	internal static bool IsNetworkMultiplayerRun()
	{
		return HextechPlayerContextHelper.IsNetworkMultiplayerRun();
	}

	protected static bool IsNetworkMultiplayer()
	{
		return IsNetworkMultiplayerRun();
	}

	protected int GetPlayerActNumberForScaling()
	{
		return HextechPlayerContextHelper.GetActNumberForScaling(Owner);
	}

	protected bool IsDefectPlayer(Player player)
	{
		return HextechPlayerContextHelper.IsDefectPlayer(player);
	}

	protected bool IsDefectOwner => Owner != null && IsDefectPlayer(Owner);

	protected bool IsIroncladPlayer(Player player)
	{
		return HextechPlayerContextHelper.IsIroncladPlayer(player);
	}

	protected bool IsSilentPlayer(Player player)
	{
		return HextechPlayerContextHelper.IsSilentPlayer(player);
	}

	protected bool IsRegentPlayer(Player player)
	{
		return HextechPlayerContextHelper.IsRegentPlayer(player);
	}

	protected bool IsRegentOwner => Owner != null && IsRegentPlayer(Owner);

	protected bool IsNecrobinderPlayer(Player player)
	{
		return HextechPlayerContextHelper.IsNecrobinderPlayer(player);
	}
}

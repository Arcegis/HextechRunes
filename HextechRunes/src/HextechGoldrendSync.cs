using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal struct HextechGoldrendGoldLostMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public ulong TargetPlayerNetId;
    public int Amount;
    public RunLocation Location;

    public readonly bool ShouldBroadcast => true;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.Debug;
    readonly RunLocation IRunLocationTargetedMessage.Location => Location;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteULong(TargetPlayerNetId);
        writer.WriteInt(Amount);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        TargetPlayerNetId = reader.ReadULong();
        Amount = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}

internal static class HextechGoldrendSync
{
    private const int GoldrendStealAmount = 10;

    private static readonly Dictionary<ulong, int> PendingCombatGoldLosses = new();

    private static INetGameService? _registeredNetService;

    public static void Install()
    {
        EnsureRegistered();
    }

    public static void EnsureRegistered()
    {
        INetGameService? netService = RunManager.Instance?.NetService;
        if (netService == null || ReferenceEquals(netService, _registeredNetService))
        {
            return;
        }

        if (_registeredNetService != null)
        {
            _registeredNetService.UnregisterMessageHandler<HextechGoldrendGoldLostMessage>(HandleGoldLostMessage);
        }

        netService.RegisterMessageHandler<HextechGoldrendGoldLostMessage>(HandleGoldLostMessage);
        _registeredNetService = netService;
    }

    public static void ResetCombat()
    {
        PendingCombatGoldLosses.Clear();
    }

    public static async Task HandleEnemyGoldrendHit(Player targetPlayer)
    {
        EnsureRegistered();

        NetGameType gameType = RunManager.Instance.NetService.Type;
        if (gameType == NetGameType.Client)
        {
            return;
        }

        if (gameType == NetGameType.Host)
        {
            TrackPendingGoldLoss(targetPlayer);
            return;
        }

        int amount = Math.Min(GoldrendStealAmount, Math.Max(0, targetPlayer.Gold));
        if (amount > 0)
        {
            await PlayerCmd.LoseGold(amount, targetPlayer, GoldLossType.Stolen);
        }
    }

    public static async Task ApplyPendingCombatGoldLosses(RunState runState)
    {
        EnsureRegistered();

        if (PendingCombatGoldLosses.Count == 0)
        {
            return;
        }

        KeyValuePair<ulong, int>[] losses = PendingCombatGoldLosses.ToArray();
        PendingCombatGoldLosses.Clear();

        if (RunManager.Instance.NetService.Type != NetGameType.Host)
        {
            return;
        }

        foreach ((ulong targetNetId, int pendingAmount) in losses)
        {
            Player? targetPlayer = runState.Players.FirstOrDefault(player => player.NetId == targetNetId);
            if (targetPlayer == null)
            {
                continue;
            }

            int amount = Math.Min(pendingAmount, Math.Max(0, targetPlayer.Gold));
            if (amount <= 0)
            {
                continue;
            }

            await PlayerCmd.LoseGold(amount, targetPlayer, GoldLossType.Stolen);
            RunManager.Instance.NetService.SendMessage(new HextechGoldrendGoldLostMessage
            {
                TargetPlayerNetId = targetNetId,
                Amount = amount,
                Location = runState.RunLocation
            });
        }
    }

    private static void TrackPendingGoldLoss(Player targetPlayer)
    {
        int alreadyPending = PendingCombatGoldLosses.GetValueOrDefault(targetPlayer.NetId, 0);
        int remainingGold = Math.Max(0, targetPlayer.Gold - alreadyPending);
        int amount = Math.Min(GoldrendStealAmount, remainingGold);
        if (amount <= 0)
        {
            return;
        }

        PendingCombatGoldLosses[targetPlayer.NetId] = alreadyPending + amount;
    }

    private static void HandleGoldLostMessage(HextechGoldrendGoldLostMessage message, ulong senderId)
    {
        if (message.Amount <= 0 || RunManager.Instance.DebugOnlyGetState() is not RunState runState)
        {
            return;
        }

        Player? targetPlayer = runState.Players.FirstOrDefault(player => player.NetId == message.TargetPlayerNetId);
        if (targetPlayer == null)
        {
            Log.Warn($"[{ModInfo.Id}][Mayhem] Goldrend sync skipped: target player {message.TargetPlayerNetId} not found sender={senderId}");
            return;
        }

        int amount = Math.Min(message.Amount, Math.Max(0, targetPlayer.Gold));
        if (amount <= 0)
        {
            return;
        }

        TaskHelper.RunSafely(PlayerCmd.LoseGold(amount, targetPlayer, GoldLossType.Stolen));
    }
}

using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace TheBetterRoles;

public static class PlayerControlDataExtension
{
    // Base
    public class ExtendedPlayerInfo
    {
        public byte _PlayerId { get; set; }
        public NetworkedPlayerInfo? _Data { get; set; }
        public string? RealName => _Data.PlayerName;
        public bool HasShowDcMsg { get; set; } = false;
        public DisconnectReasons? DisconnectReason { get; set; }
        public ExtendedRoleInfo? RoleInfo { get; set; }
    }

    public class ExtendedRoleInfo
    {
        public bool RoleAssigned => Role != null;
        public CustomRoleBehavior? Role { get; set; }
        public CustomRoles RoleType { get; set; }
        public List<CustomRoleBehavior>? Addons { get; set; } = [];
        public int Kills { get; set; } = 0;
    }

    public static List<ExtendedPlayerInfo> cachedplayerInfo = [];
    public static readonly Dictionary<NetworkedPlayerInfo, ExtendedPlayerInfo> playerInfo = [];

    public static void ClearData(this ExtendedPlayerInfo BetterData) => playerInfo[BetterData._Data] = new ExtendedPlayerInfo
    {
        _Data = BetterData._Data,
        RoleInfo = new ExtendedRoleInfo(),
    };

    // Reset info when needed
    public static void ResetPlayerData(PlayerControl player)
    {
        if (player == null) return;
    }

    // Helper method to initialize and return BetterData
    private static ExtendedPlayerInfo GetOrCreateBetterData(NetworkedPlayerInfo data)
    {
        if (!playerInfo.ContainsKey(data))
        {
            ExtendedPlayerInfo newData = new ExtendedPlayerInfo
            {
                _PlayerId = data.PlayerId,
                _Data = data,
                RoleInfo = new ExtendedRoleInfo(),
            };
            cachedplayerInfo.Add(newData);
            playerInfo[data] = newData;
            playerInfo[data].RoleInfo.Role = new CrewmateRoleTBR() { _player = Utils.PlayerFromPlayerId(data.PlayerId), _data = data };
            playerInfo[data].RoleInfo.RoleType = playerInfo[data].RoleInfo.Role.RoleType;
        }
        return playerInfo[data];
    }

    // Get BetterData from PlayerControl
    public static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        return player == null ? null : GetOrCreateBetterData(player.Data);
    }

    // Get BetterData from NetworkedPlayerInfo
    public static ExtendedPlayerInfo? BetterData(this NetworkedPlayerInfo info)
    {
        return GetOrCreateBetterData(info);
    }

    // Get BetterData from ClientData
    public static ExtendedPlayerInfo? BetterData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        return player == null ? null : GetOrCreateBetterData(player.Data);
    }

    // Get BetterData from NetworkedPlayerInfo
    public static ExtendedPlayerInfo? GetOldBetterData(this NetworkedPlayerInfo info) => cachedplayerInfo.FirstOrDefault(data => data._PlayerId == info.PlayerId);
    public static ExtendedPlayerInfo? GetOldBetterData(this BetterCachedPlayerData info) => cachedplayerInfo.FirstOrDefault(data => data._PlayerId == info.PlayerId);
}

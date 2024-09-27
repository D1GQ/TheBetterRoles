using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace TheBetterRoles;

public static class PlayerControlDataExtension
{
    // Base
    public class ExtendedPlayerInfo
    {
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
        public List<CustomRoleBehavior>? Addons { get; set; }
        public CustomRoles RoleType { get; set; }
        public int Kills { get; set; } = 0;
    }

    public static readonly Dictionary<string, ExtendedPlayerInfo> playerInfo = [];

    public static void ClearData(this ExtendedPlayerInfo BetterData) => playerInfo[BetterData._Data.Puid] = new ExtendedPlayerInfo
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
    private static ExtendedPlayerInfo GetOrCreateBetterData(string puid, NetworkedPlayerInfo data, string? realName = null)
    {
        if (!playerInfo.ContainsKey(puid))
        {
            playerInfo[puid] = new ExtendedPlayerInfo
            {
                _Data = data,
                RoleInfo = new ExtendedRoleInfo(),
            };
        }
        return playerInfo[puid];
    }

    // Get BetterData from PlayerControl
    public static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        return player == null ? null : GetOrCreateBetterData(player.Data.Puid, player.Data);
    }

    // Get BetterData from NetworkedPlayerInfo
    public static ExtendedPlayerInfo? BetterData(this NetworkedPlayerInfo info)
    {
        return GetOrCreateBetterData(info.Puid, info);
    }

    // Get BetterData from ClientData
    public static ExtendedPlayerInfo? BetterData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        return player == null ? null : GetOrCreateBetterData(player.Data.Puid, player.Data, player.Data.PlayerName);
    }

}

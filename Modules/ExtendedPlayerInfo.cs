using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace TheBetterRoles;

public static class PlayerControlDataExtension
{
    // Base
    public class ExtendedPlayerInfo
    {
        // Mod Info
        public float KickTimer { get; set; } = 8f;
        public bool HasMod { get; set; }
        public string Version { get; set; } = "";
        public bool MismatchVersion { get; set; }

        public bool IsSelf { get; set; }
        public byte _PlayerId { get; set; }
        public NetworkedPlayerInfo? _Data { get; set; }
        public string? NameColor { get; set; } = string.Empty;
        public string? RealName => _Data.PlayerName;
        public bool HasShowDcMsg { get; set; } = false;
        public DisconnectReasons? DisconnectReason { get; set; }
        public ExtendedRoleInfo? RoleInfo { get; set; }
    }

    public class ExtendedRoleInfo
    {
        public bool RoleAssigned => AllRoles.Any();
        public CustomRoleBehavior? Role { get; set; }
        public CustomRoles RoleType { get; set; }
        public List<CustomAddonBehavior>? Addons { get; set; } = [];
        public int OverrideCommonTasks { get; set; } = -1;
        public int OverrideShortTasks { get; set; } = -1;
        public int OverrideLongTasks { get; set; } = -1;
        public int Kills { get; set; } = 0;
        public List<CustomRoleBehavior> AllRoles =>
            (Addons?.Cast<CustomRoleBehavior>() ?? Enumerable.Empty<CustomRoleBehavior>())
            .Concat(Role != null ? new[] { Role } : Enumerable.Empty<CustomRoleBehavior>())
            .ToList();
    }

    public static Dictionary<NetworkedPlayerInfo, ExtendedPlayerInfo> playerInfo = [];

    public static void ClearAllData()
    {
        playerInfo.Clear();
    }

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
    private static ExtendedPlayerInfo? GetOrCreateBetterData(NetworkedPlayerInfo data)
    {
        if (data == null) return null; // Check if data is null

        if (playerInfo == null) return null; // Check if playerInfo is initialized

        if (!playerInfo.ContainsKey(data))
        {
            var existingInfo = playerInfo.Keys.FirstOrDefault(Data => Data.PlayerId == data.PlayerId);
            if (existingInfo != null)
            {
                return playerInfo[existingInfo];
            }
            else
            {
                ExtendedPlayerInfo newData = new()
                {
                    IsSelf = data == PlayerControl.LocalPlayer?.Data,
                    _PlayerId = data.PlayerId,
                    _Data = data,
                    RoleInfo = new ExtendedRoleInfo(),
                };

                playerInfo[data] = newData;

                var player = Utils.PlayerFromPlayerId(data.PlayerId);
                if (player != null)
                {
                    playerInfo[data].RoleInfo.Role = new CrewmateRoleTBR().Initialize(player);
                    playerInfo[data].RoleInfo.RoleType = playerInfo[data].RoleInfo.Role.RoleType;
                }
            }
        }
        return playerInfo[data];
    }

    // Get BetterData from PlayerControl
    public static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        return player?.Data == null ? null : GetOrCreateBetterData(player.Data);
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
        return player?.Data == null ? null : GetOrCreateBetterData(player.Data);
    }
}

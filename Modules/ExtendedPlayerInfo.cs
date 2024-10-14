using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace TheBetterRoles;

// Base
public class ExtendedPlayerInfo : MonoBehaviour
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
    public CustomRoles RoleTypeWhenAlive { get; set; }
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

public static class PlayerControlDataExtension
{
    [HarmonyPatch(typeof(NetworkedPlayerInfo))]
    class NetworkedPlayerInfoPatch
    {
        [HarmonyPatch(nameof(NetworkedPlayerInfo.Init))]
        [HarmonyPostfix]
        public static void Init_Postfix(NetworkedPlayerInfo __instance)
        {
            TryCreateExtendedData(__instance);
        }

        [HarmonyPatch(nameof(NetworkedPlayerInfo.Deserialize))]
        [HarmonyPostfix]
        public static void Deserialize_Postfix(NetworkedPlayerInfo __instance)
        {
            TryCreateExtendedData(__instance);
        }

        public static void TryCreateExtendedData(NetworkedPlayerInfo data)
        {
            if (data.BetterData() == null)
            {
                ExtendedPlayerInfo newBetterData = data.gameObject.AddComponent<ExtendedPlayerInfo>();

                newBetterData.IsSelf = data == data.AmOwner;
                newBetterData._PlayerId = data.PlayerId;
                newBetterData._Data = data;
                newBetterData.RoleInfo = new()
                {
                    Role = new CrewmateRoleTBR(),
                    RoleType = CustomRoles.Crewmate
                };
                newBetterData.RoleInfo.Role.PlayerVisionMod = newBetterData.RoleInfo.Role.BaseVisionMod;
            }
        }
    }

    // Get BetterData from PlayerControl
    public static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        return player?.Data?.GetComponent<ExtendedPlayerInfo>();
    }

    // Get BetterData from NetworkedPlayerInfo
    public static ExtendedPlayerInfo? BetterData(this NetworkedPlayerInfo data)
    {
        return data?.GetComponent<ExtendedPlayerInfo>();
    }

    // Get BetterData from ClientData
    public static ExtendedPlayerInfo? BetterData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        return player?.Data?.GetComponent<ExtendedPlayerInfo>();
    }
}

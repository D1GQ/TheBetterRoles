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

    public bool IsFakeAlive { get; set; } = false;
    public bool IsSelf => _Data?.AmOwner ?? false;
    public byte _PlayerId { get; set; }
    public NetworkedPlayerInfo? _Data { get; set; }
    public float PlayerVisionMod => RoleInfo?.Role?.BaseVisionMod != null ? RoleInfo.Role.BaseVisionMod : 1f;
    public float PlayerVisionModPlus { get; set; } = 1f;
    public string? NameColor { get; set; } = string.Empty;
    public string? RealName => _Data.PlayerName;
    public bool HasShowDcMsg { get; set; } = false;
    public DisconnectReasons? DisconnectReason { get; set; }
    public ExtendedRoleInfo? RoleInfo { get; set; }

    public void Update()
    {
        var pc = _Data?.Object;
        if (pc != null)
        {
            if (RoleInfo?.AllRoles == null) return;

            if (RoleInfo.AllRoles.Any())
            {
                foreach (var role in RoleInfo.AllRoles)
                {
                    role?.Update();
                }
            }
        }
    }

    public void FixedUpdate()
    {
        var pc = _Data?.Object;
        if (pc != null)
        {
            if (RoleInfo?.AllRoles == null) return;

            if (RoleInfo.AllRoles.Any())
            {
                foreach (var role in RoleInfo.AllRoles)
                {
                    role?.FixedUpdate();

                    if (pc.IsLocalPlayer())
                    {
                        if (role?.Buttons != null)
                        {
                            foreach (var button in role.Buttons)
                            {
                                if (button?.ActionButton == null) continue;

                                button?.FixedUpdate();
                            }
                        }
                    }
                }
            }
        }
    }
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

                newBetterData._PlayerId = data.PlayerId;
                newBetterData._Data = data;
                newBetterData.RoleInfo = new()
                {
                    Role = new CrewmateRoleTBR(),
                    RoleType = CustomRoles.Crewmate
                };
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

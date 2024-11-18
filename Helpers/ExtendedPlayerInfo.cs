using HarmonyLib;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Item;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles;

public enum DeathReasons
{
    None,
    Killed,
    Guessed,
    Misfire
}

// Base
public class ExtendedPlayerInfo : MonoBehaviour
{
    // Mod Info
    public UserData? MyUserData { get; set; } = new();
    public bool DirtyName { get; set; }
    public bool HasMod { get; set; }
    public string Version { get; set; } = "";

    public DeathReasons DeathReason { get; set; } = DeathReasons.None;
    public Color DeathReasonColor { get; set; } = Color.white;
    public bool IsFakeAlive { get; set; } = false;
    public bool IsSelf { get; set; } = false;
    public byte _PlayerId { get; set; }
    public NetworkedPlayerInfo? _Data { get; set; }
    public float PlayerVisionMod => RoleInfo?.Role?.BaseVisionMod != null ? RoleInfo.Role.BaseVisionMod : 1f;
    public float PlayerVisionModPlus { get; set; } = 1f;
    public string? NameColor { get; set; } = string.Empty;
    public string? RealName => _Data.PlayerName;

    public BoolQueue PlayerTextActiveQueue { get; set; } = new();
    public BoolQueue CamouflagedQueue { get; set; } = new();
    public BoolQueue CosmeticsActiveQueue { get; set; } = new();
    public int CamouflageBackToColor { get; set; } = 0;

    public bool HasShowDcMsg { get; set; } = false;
    public DisconnectReasons? DisconnectReason { get; set; }
    public ExtendedRoleInfo? RoleInfo { get; set; }

    public void Update()
    {
        var pc = _Data?.Object;
        if (pc != null)
        {
            if (RoleInfo.AllRoles.Any())
            {
                foreach (var role in RoleInfo.AllRoles)
                {
                    role?.BaseUpdate();
                }
            }
        }
    }

    public void FixedUpdate()
    {
        var pc = _Data?.Object;
        if (pc != null)
        {
            if (RoleInfo.AllRoles.Any())
            {
                foreach (var role in RoleInfo.AllRoles)
                {
                    role?.BaseFixedUpdate();
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

public static class PlayerDataExtension
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

                newBetterData.MyUserData = UserData.GetPlayerUserData(data);
                newBetterData._PlayerId = data.PlayerId;
                newBetterData._Data = data;
                var newRole = CustomRoleManager.CreateNewRoleInstance(role => role.RoleType == CustomRoles.Crewmate);
                newBetterData.RoleInfo = new()
                {
                    Role = newRole,
                    RoleType = CustomRoles.Crewmate
                };

                _ = new LateTask(() =>
                {
                    newBetterData.DirtyName = true;
                }, 1f, shouldLog: false);
                _ = new LateTask(() =>
                {
                    newBetterData.IsSelf = data?.Object?.IsLocalPlayer() ?? false;
                }, 3f, shouldLog: false);
            }
        }
    }

    public static void DirtyName(this PlayerControl player)
    {
        player.BetterData().DirtyName = true;
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

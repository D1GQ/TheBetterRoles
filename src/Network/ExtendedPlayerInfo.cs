using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using Reactor.Networking.Rpc;
using System.Collections;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Network;

internal class ExtendedPlayerInfo : NetworkClassMono, IMonoExtension<NetworkedPlayerInfo>
{
    public NetworkedPlayerInfo? BaseMono { get; set; }

    [HideFromIl2Cpp]
    internal UserData? MyUserData { get; set; } = ReadOnlyManager.AllUsers.First();
    internal byte UpdateNameCount { get; set; }
    internal bool HasMod { get; set; }
    internal string Version { get; set; } = "";

    internal DeathReasons DeathReason { get; set; } = DeathReasons.None;
    internal Color DeathReasonColor { get; set; } = Color.white;
    internal bool IsFakeDead { get; set; } = false;
    internal bool IsLocalData { get; set; } = false;
    internal byte _PlayerId { get; set; }
    internal float PlayerVisionMod => RoleInfo?.Role?.BaseVisionMod != null ? RoleInfo.Role.BaseVisionMod : 1f;
    internal float PlayerVisionModPlus { get; set; } = 1f;
    internal string? NameColor { get; set; } = string.Empty;
    internal string? RealName => BaseMono.PlayerName;

    internal bool HasShowDcMsg { get; set; } = false;
    internal DisconnectReasons? DisconnectReason { get; set; }

    [HideFromIl2Cpp]
    internal ExtendedRoleInfo? RoleInfo { get; set; } = new();

    private void Awake()
    {
        if (!this.RegisterExtension()) return;
        MyUserData = UserData.GetPlayerUserData(BaseMono);
        _PlayerId = BaseMono.PlayerId;
        this.StartCoroutine(CoSetLaterData());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoSetLaterData()
    {
        while (true)
        {
            while (!LobbyBehaviour.Instance)
            {
                if (ShipStatus.Instance) break;
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);

            if (this is null) break;

            var player = BaseMono?.Object;
            if (player != null)
            {
                SetUpNetworkClass(player.PlayerId, player.OwnerId);
                var roleType = GameState.IsLobby ? RoleClassTypes.LobbyBehavior : RoleClassTypes.Crewmate;
                CustomRoleManager.SetCustomRole(player, roleType, false, true);
                if (player.IsLocalPlayer())
                {
                    IsLocalData = true;
                    SetDirtyBit(2);
                }
                UpdateNameCount++;
                break;
            }

            yield return null;
        }
    }

    protected override void OnNetDestroy()
    {
        this.UnregisterExtension();
    }

    public override void Serialize(MessageWriter writer)
    {
        if (IsDirtyBitSet(2) || IsDirtyBitSet(3))
        {
            var version = Main.GetVersionText().Replace(" ", ".");
            writer.Write(version);
            HasMod = true;

            if (IsDirtyBitSet(2)) UnsetDirtyBit(2);
            if (IsDirtyBitSet(3)) UnsetDirtyBit(3);
        }

        writer.WritePacked((int)DeathReason);
        writer.Write(Colors.Color32ToHex(DeathReasonColor));
        writer.Write(IsFakeDead);
        writer.WritePacked(DisconnectReason != null ? (int)DisconnectReason : -1);

        UnsetDirtyBit(1);
    }

    public override void Deserialize(MessageReader reader)
    {
        if (SyncedBits.IsDirtyBitSet(2) || SyncedBits.IsDirtyBitSet(3))
        {
            Version = reader.ReadString();
            HasMod = true;

            UpdateNameCount++;

            if (SyncedBits.IsDirtyBitSet(3)) return;

            if (GameState.IsHost)
            {
                Rpc<RpcSyncAllSettings>.Instance.Send(new(), true);
            }

            if (!IsLocalData) PlayerControl.LocalPlayer.ExtendedData().SetDirtyBit(3);
        }

        DeathReason = (DeathReasons)reader.ReadPackedInt32();
        DeathReasonColor = Colors.HexToColor(reader.ReadString());
        IsFakeDead = reader.ReadBoolean();
        var disconnectInt = reader.ReadPackedInt32();
        DisconnectReason = disconnectInt >= 0 ? (DisconnectReasons)disconnectInt : null;
    }
}

internal class ExtendedRoleInfo
{
    internal bool RoleAssigned => AllRoles.Any();
    internal RoleClass? Role { get; set; }
    internal List<RoleClassTypes> RoleHistory { get; set; } = [];
    internal RoleClassTypes RoleType { get; set; }
    internal List<AddonClass>? Addons { get; set; } = [];
    internal List<(NetworkedPlayerInfo?, RoleClass)> TargetsForRole { get; set; } = [];
    internal int OverrideCommonTasks { get; set; } = -1;
    internal int OverrideShortTasks { get; set; } = -1;
    internal int OverrideLongTasks { get; set; } = -1;
    internal int Kills { get; set; } = 0;

    private List<RoleClass> _allRolesCache;
    private bool _isCacheValid = false;
    internal List<RoleClass> AllRoles
    {
        get
        {
            if (!_isCacheValid)
            {
                _allRolesCache = (Role != null ? new[] { Role } : Enumerable.Empty<RoleClass>())
                                .Concat(Addons?.Cast<RoleClass>() ?? Enumerable.Empty<RoleClass>())
                                .ToList();
                _isCacheValid = true;
            }
            return _allRolesCache;
        }
    }

    internal void DirtyRolesCache()
    {
        _isCacheValid = false;
    }
}

internal static class PlayerDataExtension
{
    /// <summary>
    /// Checks if the specified player is a target of another player.
    /// </summary>
    /// <param name="target">The player being checked as a target.</param>
    /// <param name="player">The player who may have the target.</param>
    /// <returns>True if the player has the target, otherwise false.</returns>
    internal static bool IsTargetOf(this PlayerControl target, PlayerControl player) =>
        player.Data?.ExtendedData().RoleInfo.TargetsForRole.Any(t => t.Item1 == target.Data) ?? false;

    /// <summary>
    /// Checks if the player has the specified target.
    /// </summary>
    /// <param name="player">The player whose targets are being checked.</param>
    /// <param name="target">The player to check as a target.</param>
    /// <returns>True if the player has the target, otherwise false.</returns>
    internal static bool HasTarget(this PlayerControl player, PlayerControl target) =>
        player.Data?.ExtendedData().RoleInfo.TargetsForRole.Any(t => t.Item1 == target.Data) ?? false;

    /// <summary>
    /// Adds a target to the player's role-specific target list.
    /// </summary>
    /// <param name="player">The player to add the target to.</param>
    /// <param name="target">The player being added as a target.</param>
    /// <param name="role">The role associated with the target.</param>
    internal static void AddTarget(this PlayerControl player, PlayerControl target, RoleClass role) =>
        player.Data.ExtendedData().RoleInfo.TargetsForRole.Add((target.Data, role));

    /// <summary>
    /// Removes a target from the player's role-specific target list.
    /// </summary>
    /// <param name="player">The player whose target is being removed.</param>
    /// <param name="target">The target to remove.</param>
    /// <param name="role">The role associated with the target.</param>
    /// <returns>True if the target was removed, otherwise false.</returns>
    internal static bool RemoveTarget(this PlayerControl player, PlayerControl target, RoleClass role) =>
        player.Data.ExtendedData().RoleInfo.TargetsForRole.Remove((target.Data, role));

    /// <summary>
    /// Clears all targets from the player's role-specific target list.
    /// </summary>
    /// <param name="player">The player whose targets should be cleared.</param>
    internal static void ClearTargets(this PlayerControl player) =>
        player.Data.ExtendedData().RoleInfo.TargetsForRole.Clear();

    /// <summary>
    /// Marks the player's name as needing an update. 
    /// Player names are not constantly updated to optimize performance and reduce FPS drops.
    /// </summary>
    /// <param name="player">The player whose name should be marked as dirty.</param>
    internal static void UpdateName(this PlayerControl player)
    {
        if (player?.Data != null)
        {
            byte count = (byte)UnityEngine.Object.FindObjectsOfType<PlayerControl>().Count;
            var edata = player.ExtendedData();
            edata?.UpdateNameCount = count;
        }
    }

    /// <summary>
    /// Marks the player's name as needing an update. 
    /// Player names are not constantly updated to optimize performance and reduce FPS drops.
    /// </summary>
    /// <param name="data">The networked player info associated with the player.</param>
    internal static void UpdateName(this NetworkedPlayerInfo data)
    {
        if (data != null)
        {
            byte count = (byte)Main.AllPlayerControls.Where(pc => pc.Data == data).Count();
            var edata = data.ExtendedData();
            edata?.UpdateNameCount = count;
        }
    }

    /// <summary>
    /// Marks the player's data as needing synchronization.  
    /// This triggers serialization and deserialization to keep the player's information up to date across the network.
    /// </summary>
    /// <param name="player">The player whose data should be marked as dirty.</param>
    internal static void DirtyData(this PlayerControl player)
    {
        if (player?.Data != null)
        {
            player.ExtendedData()?.MarkDirty();
        }
    }

    /// <summary>
    /// Marks the networked player data as needing synchronization.  
    /// This triggers serialization and deserialization to ensure the player's information is up to date across the network.
    /// </summary>
    /// <param name="data">The networked player info to mark as dirty.</param>
    internal static void DirtyData(this NetworkedPlayerInfo data)
    {
        if (data != null)
        {
            data.ExtendedData()?.MarkDirty();
        }
    }

    /// <summary>
    /// Retrieves the extended player information component from the given networked player info.
    /// </summary>
    /// <param name="data">The networked player info to get the extended data from.</param>
    /// <returns>The <see cref="ExtendedPlayerInfo"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerInfo? ExtendedData(this NetworkedPlayerInfo data)
    {
        return MonoExtensionManager.Get<ExtendedPlayerInfo>(data);
    }

    /// <summary>
    /// Retrieves the extended player information component from the given player.
    /// </summary>
    /// <param name="player">The player to get the extended data from.</param>
    /// <returns>The <see cref="ExtendedPlayerInfo"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerInfo? ExtendedData(this PlayerControl player)
    {
        return MonoExtensionManager.Get<ExtendedPlayerInfo>(player?.Data);
    }

    /// <summary>
    /// Retrieves the extended player information component from the given extended player control.
    /// </summary>
    /// <param name="extendedPlayer">The extended player control to get the extended data from.</param>
    /// <returns>The <see cref="ExtendedPlayerInfo"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerInfo? ExtendedData(this ExtendedPlayerControl extendedPlayer)
    {
        if (extendedPlayer.BaseMono?.Data == null) return null;
        return extendedPlayer.BaseMono?.Data?.ExtendedData();
    }

    /// <summary>
    /// Retrieves the extended player information component from the given client data.
    /// </summary>
    /// <param name="data">The client data to get the extended data from.</param>
    /// <returns>The <see cref="ExtendedPlayerInfo"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerInfo? ExtendedData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        if (player?.Data == null) return null;
        return player?.Data?.ExtendedData();
    }
}

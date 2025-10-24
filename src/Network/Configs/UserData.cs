using System.Text.Json.Serialization;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// This class stores user data, including name, PUID (Persistent Unique Identifier), friend code, 
/// overhead tag, color, permissions, and whether the data is local. 
/// Methods are provided to get player data from various sources, and to clone user data.
/// </summary>
internal class UserData(string name = "", string puid = "", string friendCode = "", string overheadTag = "", string overheadColor = "", ushort permissions = 0, bool isLocalData = false)
{
    /// <summary>
    /// The name of the user.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; } = name;

    /// <summary>
    /// The PUID (Persistent Unique Identifier) of the user.
    /// </summary>
    [JsonPropertyName("puid")] public string Puid { get; } = puid;

    /// <summary>
    /// The friend's code of the user.
    /// </summary>
    [JsonPropertyName("friendcode")] public string FriendCode { get; } = friendCode;

    /// <summary>
    /// The overhead tag to be displayed for the user.
    /// </summary>
    [JsonPropertyName("overheadtag")] public string OverheadTag { get; } = overheadTag;

    /// <summary>
    /// The overhead color for the user.
    /// </summary>
    [JsonPropertyName("overheadColor")] public string OverheadColor { get; } = overheadColor;

    /// <summary>
    /// The permissions assigned to the user (represented using bit flags).
    /// </summary>
    [JsonPropertyName("permissions")] public ushort Permissions { get; } = permissions;

    /// <summary>
    /// Flag indicating whether the user data is local or not.
    /// </summary>
    public bool IsLocalData { get; set; } = isLocalData;

    /// <summary>
    /// Checks if the player represented by the provided data exists in the user list and returns their cloned data.
    /// </summary>
    internal static UserData? GetPlayerUserData(NetworkedPlayerInfo data) =>
        ReadOnlyManager.AllUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode()).Clone()
        ?? ReadOnlyManager.AllUsers[0].Clone();

    /// <summary>
    /// Gets the user data for a player based on their PUID.
    /// </summary>
    internal static UserData? GetPlayerUserDataFromPuid(string puid) =>
        ReadOnlyManager.AllUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(puid)) ?? ReadOnlyManager.AllUsers.First().Clone();

    /// <summary>
    /// Gets the user data for a player based on their friend code.
    /// </summary>
    internal static UserData? GetPlayerUserDataFromFriendCode(string friendcode) =>
        ReadOnlyManager.AllUsers?.FirstOrDefault(user => user.FriendCode == Utils.GetHashStr(friendcode)) ?? ReadOnlyManager.AllUsers.First().Clone();
}

/// <summary>
/// This static class provides extension methods for working with UserData, 
/// including cloning user data, setting local data, checking permissions, 
/// and verifying the user’s account based on their PUID or friend code.
/// </summary>
internal static class UserDataExtensions
{
    /// <summary>
    /// Creates a clone of the given UserData instance with the same properties.
    /// </summary>
    internal static UserData Clone(this UserData userData) =>
        new(userData?.Name ?? "Default",
            userData?.Puid ?? "",
            userData?.FriendCode ?? "",
            userData?.OverheadTag ?? "",
            userData?.OverheadColor ?? "",
            userData?.Permissions ?? 0,
            userData?.IsLocalData ?? false);

    /// <summary>
    /// A flag to track if local data has been set.
    /// </summary>
    internal static bool HasLocalData = false;

    /// <summary>
    /// Attempts to set the local user data by checking the current user's PUID or friend code against the banned users list.
    /// </summary>
    internal static bool TrySetLocalData()
    {
        if (!HasLocalData)
        {
            if (EOSManager.Instance)
            {
                var data = ReadOnlyManager.AllUsers?.FirstOrDefault(user =>
                    user.Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) ||
                    user.FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode))?.Clone();

                if (data != null)
                {
                    Logger.Log($"Found local UserData({data.Name})");
                    data.IsLocalData = true;
                    Main.MyData = data;
                    HasLocalData = true;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the user has a specific permission using the provided permission flag.
    /// </summary>
    private static bool HasPermission(this UserData userData, MultiPermissionFlags permissionFlags)
    {
        if ((userData.Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All)
        {
            return true;
        }

        return (userData.Permissions & (ushort)permissionFlags) == (ushort)permissionFlags;
    }

    /// <summary>
    /// Checks if the user has the Developer permission.
    /// </summary>
    internal static bool IsDev(this UserData userData) => userData.HasPermission(MultiPermissionFlags.Dev);

    /// <summary>
    /// Checks if the user has the Tester permission.
    /// </summary>
    internal static bool IsTester(this UserData userData) => userData.HasPermission(MultiPermissionFlags.Tester);

    /// <summary>
    /// Checks if the user has the Tier 3 Contributor permission.
    /// </summary>
    internal static bool IsSponsorTier3(this UserData userData) => userData.HasPermission(MultiPermissionFlags.Contributor_3);

    /// <summary>
    /// Checks if the user has the Tier 2 or Tier 3 Contributor permission.
    /// </summary>
    internal static bool IsSponsorTier2(this UserData userData) => userData.HasPermission(MultiPermissionFlags.Contributor_2 | MultiPermissionFlags.Contributor_3);

    /// <summary>
    /// Checks if the user has the Tier 1, Tier 2, or Tier 3 Contributor permission.
    /// </summary>
    internal static bool IsSponsorTier1(this UserData userData) => userData.IsSponsor();

    /// <summary>
    /// Checks if the user has any Contributor permission.
    /// </summary>
    internal static bool IsSponsor(this UserData userData) => userData.HasPermission(MultiPermissionFlags.Contributor_1 | MultiPermissionFlags.Contributor_2 | MultiPermissionFlags.Contributor_3 | MultiPermissionFlags.Dev);

    /// <summary>
    /// Checks if the user has the "All" permission.
    /// </summary>
    internal static bool HasAll(this UserData userData) => (userData.Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All;

    /// <summary>
    /// Verifies the user based on the current PUID and friend code, and whether the API is connected.
    /// </summary>
    internal static bool IsVerified(this UserData userData) =>
        userData.Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) &&
        userData.FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode) &&
        GithubAPI.HasConnectedAPI;

    /// <summary>
    /// Verifies the user based on the provided player data's PUID and friend code, and whether the API is connected.
    /// </summary>
    internal static bool IsVerified(this UserData userData, NetworkedPlayerInfo data) =>
        userData.Puid == Utils.GetHashStr(data?.Puid ?? string.Empty) &&
        userData.FriendCode == Utils.GetHashStr(data?.FriendCode ?? string.Empty) &&
        GithubAPI.HasConnectedAPI;

    /// <summary>
    /// Verifies the user based on the provided player control’s PUID and friend code, and whether the API is connected.
    /// </summary>
    internal static bool IsVerified(this UserData userData, PlayerControl player) =>
        userData.Puid == Utils.GetHashStr(player?.Data?.Puid ?? string.Empty) &&
        userData.FriendCode == Utils.GetHashStr(player?.Data?.FriendCode ?? string.Empty) &&
        GithubAPI.HasConnectedAPI;
}
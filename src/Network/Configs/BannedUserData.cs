using System.Text.Json.Serialization;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// This class holds the data for banned users, including their name, PUID, friend code, and the reason for their ban.
/// It provides methods to check if a player is banned by comparing their PUID or friend code with the list of banned users.
/// The class uses JSON serialization to allow easy conversion to and from JSON format.
/// </summary>
internal class BannedUserData(string name = "", string puid = "", string friendCode = "", string reason = "")
{
    /// <summary>
    /// The name of the banned player.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; } = name;

    /// <summary>
    /// The PUID (Persistent Unique Identifier) of the banned player.
    /// </summary>
    [JsonPropertyName("puid")] public string Puid { get; } = puid;

    /// <summary>
    /// The friend code of the banned player.
    /// </summary>
    [JsonPropertyName("friendcode")] public string FriendCode { get; } = friendCode;

    /// <summary>
    /// The reason for the player's ban.
    /// </summary>
    [JsonPropertyName("reason")] public string Reason { get; } = reason;

    /// <summary>
    /// Checks if the player represented by the given data is banned by comparing their PUID or friend code with the banned users list.
    /// </summary>
    internal static bool CheckPlayerBan(NetworkedPlayerInfo data) =>
        ReadOnlyManager.AllBannedUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode()) != null;

    /// <summary>
    /// Checks if the given PUID is associated with a banned player.
    /// </summary>
    internal static bool CheckPuidBan(string puid) =>
        ReadOnlyManager.AllBannedUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(puid)) != null;

    /// <summary>
    /// Checks if the given friend code is associated with a banned player.
    /// </summary>
    internal static bool CheckFriendCodeBan(string friendcode) =>
        ReadOnlyManager.AllBannedUsers?.FirstOrDefault(user => user.FriendCode == Utils.GetHashStr(friendcode)) != null;
}

/// <summary>
/// This static class provides extension methods for checking if the local user is banned.
/// It uses the EOSManager to check if the local user's PUID or friend code matches a banned user.
/// </summary>
internal static class BannedUserDataExtensions
{
    /// <summary>
    /// A flag indicating if the local user is banned. Used for caching the result of the ban check.
    /// </summary>
    internal static bool IsBanned = false;

    /// <summary>
    /// Checks if the local user is banned by comparing their PUID or friend code with the banned users list.
    /// If the user is banned, the banned user data is returned.
    /// </summary>
    internal static bool CheckLocalBan(out BannedUserData bannedData)
    {
        bannedData = null;

        if (IsBanned) return true;

        if (EOSManager.InstanceExists)
        {
            var data = ReadOnlyManager.AllBannedUsers?.FirstOrDefault(user =>
                user.Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) ||
                user.FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode));

            if (data != null)
            {
                bannedData = data;
                IsBanned = true;
                return true;
            }
        }

        return false;
    }
}
using System.Text.Json.Serialization;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Items;

[Flags]
public enum MultiPermissionFlags : ushort
{
    Contributor_1 = 1 << 0,
    Contributor_2 = 1 << 1,
    Contributor_3 = 1 << 2,
    Tester = 1 << 3,
    Staff = 1 << 4,
    Dev = 1 << 5,
    All = 1 << 6
}

[method: JsonConstructor]
public class UserData(string name = "", string puid = "", string friendCode = "", string overheadTag = "", string overheadColor = "", ushort permissions = 0)
{
    public static List<UserData> AllUsers = [new UserData("Default")];

    public bool IsLocalData { get; private set; }

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("puid")]
    public string Puid { get; } = puid;

    [JsonPropertyName("friendcode")]
    public string FriendCode { get; } = friendCode;

    [JsonPropertyName("overheadtag")]
    public string OverheadTag { get; } = overheadTag;

    [JsonPropertyName("overheadColor")]
    public string OverheadColor { get; } = overheadColor;

    [JsonPropertyName("permissions")]
    public ushort Permissions { get; } = permissions;

    private static bool HasLocalData = false;
    public static void TrySetLocalData()
    {
        if (!HasLocalData)
        {
            if (EOSManager.Instance)
            {
                var data = AllUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) || user.FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode));
                if (data != null)
                {
                    Logger.Log($"Found local UserData({data.Name})");
                    data.IsLocalData = true;
                    Main.MyData = new(data.Name, data.Puid, data.FriendCode, data.OverheadTag, data.OverheadColor, data.Permissions)
                    {
                        IsLocalData = true
                    };
                    HasLocalData = true;
                }
            }
        }
    }
    public static UserData? GetPlayerUserData(NetworkedPlayerInfo data) => AllUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode()) ?? AllUsers.First();
    public static UserData? GetPlayerUserDataFromPuid(string puid) => AllUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(puid)) ?? AllUsers.First();
    public static UserData? GetPlayerUserDataFromFriendCode(string friendcode) => AllUsers?.FirstOrDefault(user => user.FriendCode == Utils.GetHashStr(friendcode)) ?? AllUsers.First();

    private bool HasPermission(MultiPermissionFlags permissionFlags)
    {
        if ((Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All)
        {
            return true;
        }

        return (Permissions & (ushort)permissionFlags) == (ushort)permissionFlags;
    }

    public bool IsDev() => HasPermission(MultiPermissionFlags.Dev);
    public bool IsTester() => HasPermission(MultiPermissionFlags.Tester);
    public bool IsSponsorTier3() => HasPermission(MultiPermissionFlags.Contributor_3);
    public bool IsSponsorTier2() => HasPermission(MultiPermissionFlags.Contributor_2 | MultiPermissionFlags.Contributor_3);
    public bool IsSponsorTier1() => IsSponsor();
    public bool IsSponsor() => HasPermission(MultiPermissionFlags.Contributor_1 | MultiPermissionFlags.Contributor_2 | MultiPermissionFlags.Contributor_3 | MultiPermissionFlags.Dev);
    public bool HasAll() => (Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All;
    public bool IsVerified() => Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) && FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode);
    public bool IsVerified(NetworkedPlayerInfo data) => Puid == Utils.GetHashStr(data?.Puid ?? string.Empty) && FriendCode == Utils.GetHashStr(data?.FriendCode ?? string.Empty);
    public bool IsVerified(PlayerControl player) => Puid == Utils.GetHashStr(player?.Data?.Puid ?? string.Empty) && FriendCode == Utils.GetHashStr(player?.Data?.FriendCode ?? string.Empty);
}
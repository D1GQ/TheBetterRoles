using System.Text.Json;
using System.Text.Json.Serialization;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Items;

[Flags]
public enum MultiPermissionFlags : ushort
{
    Contributor_1 = 1 << 1,
    Contributor_2 = 1 << 2,
    Contributor_3 = 1 << 3,
    Tester = 1 << 4,
    Staff = 1 << 5,
    Dev = 1 << 6,
    All = 1 << 7
}

public class UserData
{
    public static List<UserData> AllUsers = [];
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("puid")] public string Puid { get; set; }
    [JsonPropertyName("friendcode")] public string FriendCode { get; set; }
    [JsonPropertyName("overheadtag")] public string OverheadTag { get; set; }
    [JsonPropertyName("overheadColor")] public string OverheadColor { get; set; }
    [JsonPropertyName("permissions")] public ushort Permissions { get; set; }

    public static UserData? GetPlayerUserData(NetworkedPlayerInfo data) => AllUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode());

    public bool HasPermission(MultiPermissionFlags permissionFlags)
    {
        if ((Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All)
        {
            return true;
        }

        return (Permissions & (ushort)permissionFlags) == (ushort)permissionFlags;
    }
}

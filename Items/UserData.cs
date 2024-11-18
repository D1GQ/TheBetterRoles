using System.Text.Json;
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
    public static List<UserData> AllUsers = [new UserData()];

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
                    data.IsLocalData = true;
                    Main.MyData = data;
                    HasLocalData = true;
                }
            }
        }
    }
    public static UserData? GetPlayerUserData(NetworkedPlayerInfo data) => AllUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode());
    public static UserData? GetPlayerUserDataFromPuid(string puid) => AllUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(puid));
    public static UserData? GetPlayerUserDataFromFriendCode(string friendcode) => AllUsers?.FirstOrDefault(user => user.FriendCode == Utils.GetHashStr(friendcode));

    public bool HasPermission(MultiPermissionFlags permissionFlags)
    {
        if ((Permissions & (ushort)MultiPermissionFlags.All) == (ushort)MultiPermissionFlags.All)
        {
            return true;
        }

        return (Permissions & (ushort)permissionFlags) == (ushort)permissionFlags;
    }
}
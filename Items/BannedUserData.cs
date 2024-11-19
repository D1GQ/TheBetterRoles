using System.Text.Json.Serialization;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Items;

[method: JsonConstructor]
public class BannedUserData(string name = "", string puid = "", string friendCode = "", string reason = "")
{
    public static List<BannedUserData> AllBannedUsers = [new BannedUserData("Default")];

    public bool IsLocalBan { get; private set; }

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("puid")]
    public string Puid { get; } = puid;

    [JsonPropertyName("friendcode")]
    public string FriendCode { get; } = friendCode;

    [JsonPropertyName("reason")]
    public string Reason { get; } = reason;

    private static bool IsBanned = false;
    public static bool CheckLocalBan()
    {
        if (IsBanned) return true;

        if (EOSManager.Instance)
        {
            var data = AllBannedUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(EOSManager.Instance.ProductUserId) || user.FriendCode == Utils.GetHashStr(EOSManager.Instance.FriendCode));
            if (data != null)
            {
                data.IsLocalBan = true;
                IsBanned = true;
                return true;
            }
        }

        return false;
    }
    public static bool CheckPlayerBan(NetworkedPlayerInfo data) => AllBannedUsers?.FirstOrDefault(user => user.Puid == data.GetHashPuid() || user.FriendCode == data.GetHashFriendcode()) != null;
    public static bool CheckPuidBan(string puid) => AllBannedUsers?.FirstOrDefault(user => user.Puid == Utils.GetHashStr(puid)) != null;
    public static bool CheckFriendCodeBan(string friendcode) => AllBannedUsers?.FirstOrDefault(user => user.FriendCode == Utils.GetHashStr(friendcode)) != null;
}
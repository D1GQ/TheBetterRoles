using AmongUs.Data;
using Discord;
using HarmonyLib;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches;


[HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
public class DiscordRPC
{
    private static string lobbycode = "";
    private static string region = "";

    public static void Prefix([HarmonyArgument(0)] Activity activity)
    {
        if (activity == null) return;

        string details = $"TBR {Main.GetVersionText()}";
        activity.Details = details;

        if (activity.State == "In Menus") return;

        try
        {
            if (!DataManager.Settings.Gameplay.StreamerMode)
            {
                UpdateRegionAndLobbyCode();
                if (!string.IsNullOrEmpty(lobbycode) && !string.IsNullOrEmpty(region))
                {
                    if (GameState.IsNormalGame)
                        details = $"TBR - {lobbycode} ({region})";
                    else if (GameState.IsHideNSeek)
                        details = $"TBR Hide & Seek - {lobbycode} ({region})";
                }
            }
            else
            {
                if (GameState.IsHideNSeek)
                    details = $"TBR v{Main.PluginVersion} - Hide & Seek";
            }
        }
        catch
        {
        }

        activity.Details = details;
    }

    private static void UpdateRegionAndLobbyCode()
    {
        if (GameState.IsLobby)
        {
            lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
            region = ServerManager.Instance.CurrentRegion.Name;
            region = region switch
            {
                "North America" => "NA",
                "Europe" => "EU",
                "Asia" => "AS",
                _ when region.Contains("MNA") => "MNA",
                _ when region.Contains("MEU") => "MEU",
                _ when region.Contains("MAS") => "MAS",
                _ => region
            };
        }
    }
}

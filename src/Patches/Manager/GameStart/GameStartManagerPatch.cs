using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using TheBetterRoles.Network;

namespace TheBetterRoles.Patches.Manager.GameStart;

[HarmonyPatch(typeof(GameStartManager))]
internal class GameStartManagerPatch
{
    [HarmonyPatch(nameof(GameStartManager.UpdateStreamerModeUI))]
    [HarmonyPrefix]
    private static bool UpdateStreamerModeUI_Prefix(GameStartManager __instance)
    {
        string text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
        text ??= string.Empty;
        __instance.GameRoomNameCode.text = DataManager.Settings.Gameplay.StreamerMode && text != string.Empty ? "<#4FAFFF>TBR</color>" : text;
        return false;
    }

    [HarmonyPatch(nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(GameStartManager __instance)
    {
        CatchedGameData.lobbyTimer = 600f;
        __instance.UpdateStreamerModeUI();
    }

    [HarmonyPatch(nameof(GameStartManager.Update))]
    [HarmonyPrefix]
    private static void Update_Prefix(GameStartManager __instance)
    {
        __instance.MinPlayers = 0;
    }

    [HarmonyPatch(nameof(GameStartManager.UpdateMapImage))]
    [HarmonyPrefix]
    private static bool UpdateMapImage_Prefix(GameStartManager __instance, [HarmonyArgument(0)] MapNames map)
    {
        if (__instance.AllMapIcons.ToArray().FirstOrDefault(m => m.Name == map).MapIcon == null)
        {
            __instance.MapImage.sprite = __instance.AllMapIcons.ToArray().First().MapIcon;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(GameStartManager.CheckSettingsDiffs))]
    [HarmonyPrefix]
    private static bool CheckSettingsDiffs_Prefix(GameStartManager __instance)
    {
        return false;
    }
}

using HarmonyLib;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(PlayerControl))]
class PlayerControlPatch
{
    public static float time = 0f;
    [HarmonyPatch(nameof(PlayerControl.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    public static void Start_Postfix(PlayerControl __instance)
    {
        // Set up player text info
        var nameTextTransform = __instance.gameObject.transform.Find("Names/NameText_TMP");
        var nameText = nameTextTransform?.gameObject;

        void InstantiatePlayerInfoText(string name, Vector3 positionOffset)
        {
            var newTextObject = UnityEngine.Object.Instantiate(nameText, nameTextTransform);
            newTextObject.name = name;
            newTextObject.transform.DestroyChildren();
            newTextObject.transform.position += positionOffset;
            var textMesh = newTextObject.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = string.Empty;
            }
            newTextObject.SetActive(true);
        }

        InstantiatePlayerInfoText("InfoText_Info_TMP", new Vector3(0f, 0.25f));
        InstantiatePlayerInfoText("InfoText_T_TMP", new Vector3(0f, 0.15f));
        InstantiatePlayerInfoText("InfoText_B_TMP", new Vector3(0f, -0.15f));

        __instance.DirtyName();
    }

    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void FixedUpdate_Prefix(PlayerControl __instance)
    {
        SetPlayerInfo(__instance);

        // Set color blind text on player
        if (__instance.DataIsCollected())
        {
            __instance.cosmetics.SetColorBlindColor(__instance.CurrentOutfit.ColorId);
        }
        else
        {
            __instance.cosmetics.colorBlindText.text = string.Empty;
        }

        if (GameState.IsHost && GameState.IsLobby)
        {
            ExtendedPlayerInfo? betterData = __instance?.BetterData();

            if (!__instance.IsHost())
            {
                if (betterData?.HasMod == false || betterData?.MismatchVersion == true)
                {
                    betterData.KickTimer -= Time.deltaTime;
                    if (betterData?.KickTimer <= 0)
                    {
                        __instance.Kick();
                    }
                }
            }
        }
    }

    public static void SetPlayerInfo(PlayerControl player)
    {
        if (player?.Data == null || player?.BetterData()?.DirtyName == false) return;

        var playerData = player.BetterData();
        var cosmetics = player.cosmetics;

        var sbTag = new StringBuilder();
        var sbTagTop = new StringBuilder();
        var sbTagBottom = new StringBuilder();

        bool isLobbyState = GameState.IsLobby && !GameState.IsFreePlay;
        bool isLocalPlayerAlive = PlayerControl.LocalPlayer.IsAlive(true);
        bool isLocalPlayer = player.IsLocalPlayer();

        if (isLobbyState)
        {
            cosmetics.nameText.color = playerData.HasMod || isLocalPlayer
                ? new Color(0.47f, 1f, 0.95f, 1f)
                : Color.white;

            if (playerData.MismatchVersion)
            {
                sbTag.Append($"<color=#FF0800>{playerData.Version}");
                if (GameState.IsHost)
                    sbTag.Append($" - {(int)playerData.KickTimer}s");
                sbTag.Append("</color>+++");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(playerData.NameColor))
            {
                var color = Utils.HexToColor32(playerData.NameColor);
                cosmetics.nameText.color = new Color(color.r, color.g, color.b, cosmetics.nameText.color.a);
            }
            else
            {
                cosmetics.nameText.color = new Color(1f, 1f, 1f, cosmetics.nameText.color.a);
            }

            bool canRevealRole = isLocalPlayer || !isLocalPlayerAlive || player.IsImpostorTeammate() ||
                                 CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerRole(player));

            if (canRevealRole)
            {
                sbTag.Append($"{player.GetRoleNameAndColor()}{player.FormatTasksToText()}---");
            }

            bool canRevealAddons = isLocalPlayer || !isLocalPlayerAlive || player.IsImpostorTeammate() ||
                                   CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerAddons(player));

            if (canRevealAddons)
            {
                foreach (var addon in playerData.RoleInfo.Addons)
                {
                    sbTagTop.Append($"<size=55%><color={addon.RoleColor}>{addon.RoleName}</color></size>+++");
                }
            }
        }

        // Format and set the strings
        player.RawSetName(Utils.FormatPlayerName(player.Data));
        if (sbTagTop.Length > 0) player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTagTop).ToString());
        if (sbTagBottom.Length > 0) player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTagBottom).ToString(), isBottom: true);
        if (sbTag.Length > 0) player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTag).ToString(), isInfo: true);
    }
}

[HarmonyPatch(typeof(PlayerPhysics))]
public class PlayerPhysicsPatch
{
    [HarmonyPatch("get_SpeedMod")]
    [HarmonyPrefix]
    private static bool SpeedMod_Prefix(PlayerPhysics __instance, ref float __result)
    {
        if (GameManager.Instance == null)
        {
            __result = 1f;
            return false;
        }

        float playerSpeedMod = GameManager.Instance.LogicOptions.GetPlayerSpeedMod(__instance.myPlayer);

        if (__instance.myPlayer != null && __instance.myPlayer.Data != null && !__instance.myPlayer.IsAlive(true))
        {
            __result = playerSpeedMod * __instance.GhostSpeed / __instance.Speed;
            return false;
        }

        __result = playerSpeedMod;
        return false;
    }

    [HarmonyPatch(nameof(PlayerPhysics.BootFromVent))]
    [HarmonyPostfix]
    private static void BootFromVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        TBRLogger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has been booted from vent: {ventId}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerPhysics.CoEnterVent))]
    [HarmonyPostfix]
    private static void CoEnterVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        TBRLogger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has entered vent: {ventId}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerPhysics.CoExitVent))]
    [HarmonyPostfix]
    private static void CoExitVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        TBRLogger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has exit vent: {ventId}", "EventLog");
    }
}

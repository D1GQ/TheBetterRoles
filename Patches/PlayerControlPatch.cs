using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Linq;
using LibCpp2IL;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(PlayerControl))]
class PlayerControlPatch
{
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    [HarmonyPrefix]
    public static void FixedUpdate_Prefix(PlayerControl __instance)
    {
        // Set up player text info
        var nameTextTransform = __instance.gameObject.transform.Find("Names/NameText_TMP");
        var nameText = nameTextTransform?.gameObject;
        var infoText = nameTextTransform?.Find("InfoText_T_TMP");

        if (nameText != null && infoText == null)
        {
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
        }

        // Set color blind text on player
        if (__instance.DataIsCollected() && !__instance.shapeshifting)
        {
            __instance.cosmetics.SetColorBlindColor(__instance.CurrentOutfit.ColorId);
        }
        else
        {
            __instance.cosmetics.colorBlindText.text = string.Empty;
        }


        SetPlayerInfo(__instance);

        // Reset player data in lobby
        PlayerControlDataExtension.ResetPlayerData(__instance);

        if (GameStates.IsHost && GameStates.IsLobby)
        {
            if (!__instance.IsHost())
            {
                if (__instance.BetterData().HasMod == false || __instance.BetterData().MismatchVersion == true)
                {
                    __instance.BetterData().KickTimer -= Time.deltaTime;
                    if (__instance.BetterData().KickTimer <= 0)
                    {
                        __instance.Kick();
                    }
                }
            }
        }
    }

    public static void SetPlayerInfo(PlayerControl player)
    {
        try
        {
            if (player == null || player.Data == null) return;

            var sbTag = new StringBuilder();
            var sbTagTop = new StringBuilder();
            var sbTagBottom = new StringBuilder();

            if (GameStates.IsLobby && !GameStates.IsFreePlay)
            {
                if (player.BetterData().HasMod || player.IsLocalPlayer())
                {
                    player.cosmetics.nameText.color = new Color(0.47f, 1f, 0.95f, 1f);
                }
                else
                {
                    player.cosmetics.nameText.color = new Color(1f, 1f, 1f, 1f);
                }

                if (player.BetterData().MismatchVersion)
                {
                    if (GameStates.IsHost)
                    {
                        sbTag.Append($"<color=#FF0800>{player.BetterData().Version} - {(int)player.BetterData().KickTimer}s</color>+++");
                    }
                    else
                    {
                        sbTag.Append($"<color=#FF0800>{player.BetterData().Version}</color>+++");
                    }
                }
            }
            else if (GameStates.IsInGamePlay)
            {
                if (player.IsLocalPlayer() || player.IsImpostorTeammate())
                {
                    sbTag.Append($"{player.GetRoleNameAndColor()}---");

                    foreach (var addon in player.BetterData().RoleInfo.Addons)
                    {
                        sbTagTop.Append($"<size=55%><color={addon.RoleColor}>{addon.RoleName}</color></size>+++");
                    }
                }
            }

            sbTag = Utils.FormatStringBuilder(sbTag);
            sbTagTop = Utils.FormatStringBuilder(sbTagTop);
            sbTagBottom = Utils.FormatStringBuilder(sbTagBottom);

            player.SetPlayerTextInfo(sbTagTop.ToString());
            player.SetPlayerTextInfo(sbTagBottom.ToString(), isBottom: true);
            player.SetPlayerTextInfo(sbTag.ToString(), isInfo: true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    [HarmonyPatch(nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void MurderPlayer_Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (target == null) return;

        Logger.LogPrivate($"{__instance.Data.PlayerName} Has killed {target.Data.PlayerName} as {Translator.GetString(__instance.Data.Role.StringName)}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerControl.Shapeshift))]
    [HarmonyPostfix]
    public static void Shapeshift_Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool animate)
    {
        if (target == null) return;

        if (__instance != target)
            Logger.LogPrivate($"{__instance.Data.PlayerName} Has Shapeshifted into {target.Data.PlayerName}, did animate: {animate}", "EventLog");
        else
            Logger.LogPrivate($"{__instance.Data.PlayerName} Has Un-Shapeshifted, did animate: {animate}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerControl.SetRoleInvisibility))]
    [HarmonyPostfix]
    public static void SetRoleInvisibility_Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool isActive, [HarmonyArgument(1)] bool animate)
    {

        if (isActive)
            Logger.LogPrivate($"{__instance.Data.PlayerName} Has Vanished as Phantom, did animate: {animate}", "EventLog");
        else
            Logger.LogPrivate($"{__instance.Data.PlayerName} Has Appeared as Phantom, did animate: {animate}", "EventLog");
    }
}

[HarmonyPatch(typeof(PlayerPhysics))]
public class PlayerPhysicsPatch
{
    [HarmonyPatch(nameof(PlayerPhysics.BootFromVent))]
    [HarmonyPostfix]
    private static void BootFromVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has been booted from vent: {ventId}, as {Translator.GetString(__instance.myPlayer.Data.Role.StringName)}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerPhysics.CoEnterVent))]
    [HarmonyPostfix]
    private static void CoEnterVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has entered vent: {ventId}, as {Translator.GetString(__instance.myPlayer.Data.Role.StringName)}", "EventLog");
    }
    [HarmonyPatch(nameof(PlayerPhysics.CoExitVent))]
    [HarmonyPostfix]
    private static void CoExitVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has exit vent: {ventId}, as {Translator.GetString(__instance.myPlayer.Data.Role.StringName)}", "EventLog");
    }
}

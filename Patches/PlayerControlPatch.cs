using HarmonyLib;
using System.Text;
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
    }

    public static Dictionary<byte, float> Times = [];
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void FixedUpdate_Prefix(PlayerControl __instance)
    {
        if (!Times.ContainsKey(__instance.PlayerId))
        {
            Times[__instance.PlayerId] = 0f;
        }

        Times[__instance.PlayerId] += Time.deltaTime;
        if (Times[__instance.PlayerId] > 0.65f)
        {
            SetPlayerInfo(__instance);
        }

        // Set color blind text on player
        if (__instance.DataIsCollected())
        {
            __instance.cosmetics.SetColorBlindColor(__instance.CurrentOutfit.ColorId);
        }
        else
        {
            __instance.cosmetics.colorBlindText.text = string.Empty;
        }

        if (GameStates.IsHost && GameStates.IsLobby)
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
        if (player == null || player.Data == null) return;

        var playerData = player.BetterData();
        var cosmetics = player.cosmetics;
        var sbTag = new StringBuilder();
        var sbTagTop = new StringBuilder();
        var sbTagBottom = new StringBuilder();

        if (GameStates.IsLobby && !GameStates.IsFreePlay)
        {
            cosmetics.nameText.color = playerData.HasMod || player.IsLocalPlayer()
                ? new Color(0.47f, 1f, 0.95f, 1f)
                : Color.white;

            if (playerData.MismatchVersion)
            {
                sbTag.Append($"<color=#FF0800>{playerData.Version} {(GameStates.IsHost ? $"- {(int)playerData.KickTimer}s" : string.Empty)}</color>+++");
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

            if (player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) ||
                player.IsImpostorTeammate() || CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerRole(player)))
            {
                sbTag.Append($"{player.Role()?.RoleNameAndAbilityAmount}{player.FormatTasksToText()}---");
            }

            if (player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) ||
                player.IsImpostorTeammate() || CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerAddons(player)))
            {
                foreach (var addon in playerData.RoleInfo.Addons)
                {
                    sbTagTop.Append($"<size=55%>{addon.RoleNameAndAbilityAmount}</size>+++");
                }
            }
        }

        if (sbTag.Length > 0) sbTag = Utils.FormatStringBuilder(sbTag);
        if (sbTagTop.Length > 0) sbTagTop = Utils.FormatStringBuilder(sbTagTop);
        if (sbTagBottom.Length > 0) sbTagBottom = Utils.FormatStringBuilder(sbTagBottom);

        player.RawSetName(Utils.FormatPlayerName(player.Data));
        player.SetPlayerTextInfo(sbTagTop.ToString());
        player.SetPlayerTextInfo(sbTagBottom.ToString(), isBottom: true);
        player.SetPlayerTextInfo(sbTag.ToString(), isInfo: true);
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

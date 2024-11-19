using HarmonyLib;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

public class ClientPatch
{
    // Show warning message for newer and older versions of among us
    [HarmonyPatch(typeof(EOSManager))]
    public class EOSManagerPatch
    {
        [HarmonyPatch(nameof(EOSManager.EndFinalPartsOfLoginFlow))]
        [HarmonyPostfix]
        public static void EndFinalPartsOfLoginFlow_Postfix()
        {
            UserData.TrySetLocalData();

            var varSupportedVersions = Main.SupportedAmongUsVersions;
            Version currentVersion = new Version(Main.AmongUsVersion);
            Version firstSupportedVersion = new Version(varSupportedVersions.First());
            Version lastSupportedVersion = new Version(varSupportedVersions.Last());

            if (currentVersion > firstSupportedVersion)
            {
                var verText = $"<b>{varSupportedVersions.First()}</b>";
                if (firstSupportedVersion != lastSupportedVersion)
                {
                    verText = $"<b>{varSupportedVersions.Last()}</b> - <b>{varSupportedVersions.First()}</b>";
                }
                Utils.ShowPopUp($"<size=200%>-= <color=#ff2200><b>Warning</b></color> =-</size>\n\n" +
                    $"<size=125%><color=#0dff00>The Better Roles {Main.GetVersionText()}</color>\nsupports <color=#4f92ff>Among Us {verText}</color>,\n" +
                    $"<color=#4f92ff>Among Us <b>{Main.AmongUsVersion}</b></color> is above the supported versions!\n" +
                    $"<color=#ae1700>You may encounter minor to game breaking bugs.</color></size>");
            }
            else if (currentVersion < lastSupportedVersion)
            {
                var verText = $"<b>{varSupportedVersions.First()}</b>";
                if (firstSupportedVersion != lastSupportedVersion)
                {
                    verText = $"<b>{varSupportedVersions.Last()}</b> - <b>{varSupportedVersions.First()}</b>";
                }
                Utils.ShowPopUp($"<size=200%>-= <color=#ff2200><b>Warning</b></color> =-</size>\n\n" +
                    $"<size=125%><color=#0dff00>The Better Roles {Main.GetVersionText()}</color>\nsupports <color=#4f92ff>Among Us {verText}</color>,\n" +
                    $"<color=#4f92ff>Among Us <b>{Main.AmongUsVersion}</b></color> is below the supported versions!\n" +
                    $"<color=#ae1700>You may encounter minor to game breaking bugs.</color></size>");
            }
        }

        [HarmonyPatch(nameof(EOSManager.StartInitialLoginFlow))]
        [HarmonyPostfix]
        public static void StartInitialLoginFlow_Postfix(EOSManager __instance)
        {
            // __instance.StartCoroutine(DataBaseConnect.Init());
        }
    }

    // If developer set account status color to Blue
    [HarmonyPatch(typeof(SignInStatusComponent))]
    public class SignInStatusComponentPatch
    {
        [HarmonyPatch(nameof(SignInStatusComponent.SetOnline))]
        [HarmonyPrefix]
        public static bool SetOnline_Prefix(SignInStatusComponent __instance)
        {
            if (BannedUserData.CheckLocalBan())
            {
                __instance.statusSprite.sprite = __instance.guestSprite;
                __instance.glowSprite.sprite = __instance.guestGlow;
                __instance.statusSprite.color = Color.red;
                __instance.glowSprite.color = Color.red;
                Utils.DisconnectAccountFromOnline();

                return false;
            }

            if (Main.MyData.IsDev())
            {
                __instance.statusSprite.sprite = __instance.guestSprite;
                __instance.glowSprite.sprite = __instance.guestGlow;
                __instance.statusSprite.color = Color.cyan;
                __instance.glowSprite.color = Color.cyan;
                __instance.friendsButton.SetActive(true);

                return false;
            }

            return true;
        }
    }

    // Log game exit
    [HarmonyPatch(typeof(AmongUsClient))]
    public class AmongUsClientPatch
    {
        [HarmonyPatch(nameof(AmongUsClient.ExitGame))]
        [HarmonyPostfix]
        public static void ExitGame_Postfix([HarmonyArgument(0)] DisconnectReasons reason)
        {
            Logger.Log($"Client has left game for: {Enum.GetName(reason)}", "AmongUsClientPatch");
        }
    }
    [HarmonyPatch(typeof(InnerNetClient))]
    public class InnerNetClientPatch
    {
        [HarmonyPatch(nameof(InnerNetClient.CanBan))]
        [HarmonyPrefix]
        public static bool CanBan_Prefix(ref bool __result)
        {
            __result = GameState.IsHost;
            return false;
        }
        [HarmonyPatch(nameof(InnerNetClient.CanKick))]
        [HarmonyPrefix]
        public static bool CanKick_Prefix(ref bool __result)
        {
            __result = GameState.IsHost || (GameState.IsInGamePlay && (GameState.IsMeeting || GameState.IsExilling));
            return false;
        }
    }
    // Set text color
    [HarmonyPatch(typeof(CosmeticsLayer))]
    public class CosmeticsLayerPatch
    {
        [HarmonyPatch(nameof(CosmeticsLayer.SetColorBlindColor))]
        [HarmonyPrefix]
        public static bool SetColorBlindColor_Prefix(CosmeticsLayer __instance, [HarmonyArgument(0)] int color)
        {
            try
            {
                if (__instance.colorBlindText == null || !__instance.showColorBlindText)
                {
                    return false;
                }
                __instance.colorBlindText.text = __instance.GetColorBlindText();
                __instance.colorBlindText.color = Palette.PlayerColors[color];
                var player = Main.AllPlayerControls.FirstOrDefault(pc => pc.cosmetics == __instance);

                if (player != null)
                {
                    // Set new pos
                    if (!player.onLadder && !player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                    {
                        __instance.colorBlindText.transform.localPosition = new Vector3(0f, -1.5f, 0.4999f);
                    }
                    else // Set pos when on a ladder
                    {
                        __instance.colorBlindText.transform.localPosition = new Vector3(0f, -1.75f, 0.4999f);
                    }
                }
                else // Set new pos if player is null
                {
                    __instance.colorBlindText.transform.localPosition = new Vector3(0f, -1.5f, 0.4999f);
                }
            }
            catch { }

            return false;
        }
    }
    // Clean up menu
    [HarmonyPatch(typeof(RegionMenu))]
    public class RegionMenuPatch
    {
        [HarmonyPatch(nameof(RegionMenu.OnEnable))]
        [HarmonyPostfix]
        public static void AdjustButtonPositions_Postfix(RegionMenu __instance)
        {
            const int maxColumns = 4;
            int buttonsPerColumn = 6;
            float buttonSpacing = 0.6f;
            float buttonSpacingSide = 2.25f;

            List<UiElement> buttons = __instance.controllerSelectable.ToArray().ToList();

            int columnCount = (buttons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

            while (columnCount > maxColumns)
            {
                buttonsPerColumn++;
                columnCount = (buttons.Count + buttonsPerColumn - 1) / buttonsPerColumn;
            }

            float totalWidth = (columnCount - 1) * buttonSpacingSide;
            float totalHeight = (buttonsPerColumn - 1) * buttonSpacing;

            Vector3 startPosition = new Vector3(-totalWidth / 2, totalHeight / 2, 0f);

            for (int i = 0; i < buttons.Count; i++)
            {
                int col = i / buttonsPerColumn;
                int row = i % buttonsPerColumn;
                buttons[i].transform.localPosition = startPosition + new Vector3(col * buttonSpacingSide, -row * buttonSpacing, 0f);
            }
        }
    }
    // fix report name on anti cheat ban
    [HarmonyPatch(typeof(ReportReasonScreen))]
    class ReportReasonScreenPatch
    {
        [HarmonyPatch(nameof(ReportReasonScreen.Show))]
        [HarmonyPrefix]
        public static void Show_Prefix(ref string playerName)
        {
            if (Utils.IsHtmlText(playerName))
            {
                string extractedText = playerName.Split(new[] { "<color=#ffea00>", "</color>" }, StringSplitOptions.None)[1];
                playerName = extractedText;
            }
        }
    }
}

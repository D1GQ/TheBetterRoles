using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using UnityEngine;

namespace TheBetterRoles.Patches.Manager.EOS;

[HarmonyPatch(typeof(SignInStatusComponent))]
internal class SignInStatusComponentPatch
{
    [HarmonyPatch(nameof(SignInStatusComponent.SetOnline))]
    [HarmonyPrefix]
    private static bool SetOnline_Prefix(SignInStatusComponent __instance)
    {
        var varSupportedVersions = Main.SupportedAmongUsVersions;
        Version currentVersion = new(Main.AppVersion);
        Version firstSupportedVersion = new(varSupportedVersions.First());
        Version lastSupportedVersion = new(varSupportedVersions.Last());

        if (currentVersion > firstSupportedVersion)
        {
            var verText = $"<b>{varSupportedVersions.First()}</b>";
            if (firstSupportedVersion != lastSupportedVersion)
            {
                verText = $"<b>{varSupportedVersions.Last()}</b> - <b>{varSupportedVersions.First()}</b>";
            }
            Utils.ShowPopUp($"<size=200%>-= <color=#ff2200><b>Warning</b></color> =-</size>\n\n" +
                $"<size=125%><color=#00dbdb>The Better Roles {Main.GetVersionText()}</color>\nsupports <color=#4f92ff>Among Us {verText}</color>,\n" +
                $"<color=#4f92ff>Among Us <b>{Main.AppVersion}</b></color> is above the supported versions!\n" +
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
                $"<size=125%><color=#00dbdb>The Better Roles {Main.GetVersionText()}</color>\nsupports <color=#4f92ff>Among Us {verText}</color>,\n" +
                $"<color=#4f92ff>Among Us <b>{Main.AppVersion}</b></color> is below the supported versions!\n" +
                $"<color=#ae1700>You may encounter minor to game breaking bugs.</color></size>");
        }

        var lines = "<color=#ebbd34>----------------------------------------------------------------------------------------------</color>";
        if (!FileChecker.HasShownWarning && FileChecker.HasUnauthorizedFileOrMod)
        {
            Utils.ShowPopUp($"{lines}\n<b><size=200%><color=#00A3FF>{Translator.GetString("TheBetterRoles")}</color></size></b>\n<color=#757575><u><size=150%>{FileChecker.WarningMsg}</size></u>\n{lines}");
            FileChecker.HasShownWarning = true;
        }

        if (BannedUserDataExtensions.CheckLocalBan(out var bannedData))
        {
            __instance.statusSprite.sprite = __instance.guestSprite;
            __instance.glowSprite.sprite = __instance.guestGlow;
            __instance.statusSprite.color = Color.red;
            __instance.glowSprite.color = Color.red;
            __instance.friendsButton.SetActive(false);

            var reason = bannedData.Reason;
            Utils.ShowPopUp($"{lines}\n<b><size=200%><color=#00A3FF>{Translator.GetString("TheBetterRoles")}</color></size></b>\n<color=#757575><u><size=150%><color=#8f0000>You have been banned\nReason: {reason}</color></size></u>\n{lines}");

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

        if (ModInfo.IsGuestBuild)
        {
            __instance.statusSprite.sprite = __instance.guestSprite;
            __instance.glowSprite.sprite = __instance.guestGlow;
            __instance.statusSprite.material.shader = AssetBundles.GrayscaleShader;
            __instance.glowSprite.material.shader = AssetBundles.GrayscaleShader;
            __instance.statusSprite.material.SetColor("_Color", Color.yellow);
            __instance.glowSprite.material.SetColor("_Color", Color.yellow);
            __instance.glowSprite.sprite = __instance.guestGlow;
            __instance.friendsButton.SetActive(true);
            return false;
        }

        return true;
    }
}

using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(VersionShower))]
internal class VersionShowerPatch
{
    [HarmonyPatch(nameof(VersionShower.Start))]
    [HarmonyPostfix]
    private static void Postfix(VersionShower __instance)
    {
        string tbr = Translator.GetString("TBR");
        __instance.text.text = $"<color=#00dbdb>{tbr} {Main.GetVersionText()}</color> <color=#ababab>~</color> {Utils.GetPlatformName(Main.PlatformData.Platform)} v{Main.AmongUsVersion} ({Main.AppVersion})";

#if DEBUG_MULTIACCOUNTS
        __instance.text.text += $" <color=#ababab>~</color> (<#800094>MultiAccounts</color>)";
#endif

        if (ModInfo.IsGuestBuild)
        {
            __instance.text.text += $" <color=#ababab>~</color> (<#B5B500>GuestBuild</color>)";
        }
    }
}
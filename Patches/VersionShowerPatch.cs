using HarmonyLib;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(VersionShower))]
public class VersionShowerPatch
{
    [HarmonyPatch(nameof(VersionShower.Start))]
    [HarmonyPostfix]
    public static void Postfix(VersionShower __instance)
    {
        string tbr = Translator.GetString("TBR");
        __instance.text.text = $"<color=#00dbdb>{tbr} {Main.GetVersionText()}</color> <color=#ababab>~</color> {Utils.GetPlatformName(Main.PlatformData.Platform)} v{Main.AmongUsVersion}";
    }
}

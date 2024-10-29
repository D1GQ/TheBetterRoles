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
        string bau = Translator.GetString("TBR");
        __instance.text.text = $"<color=#0dff00>{bau} {Main.GetVersionText()}</color> <color=#ababab>~</color> {Utils.GetPlatformName(Main.PlatformData.Platform)} v{Main.AmongUsVersion}";
    }
}

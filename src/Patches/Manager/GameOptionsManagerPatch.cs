using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;

namespace TheBetterRoles.Patches.Manager;

// Preload modified vanilla options
[HarmonyPatch(typeof(GameOptionsManager))]
internal class GameOptionsManagerPatch
{
    [HarmonyPatch(nameof(GameOptionsManager.Initialize))]
    [HarmonyPostfix]
    private static void CreateSettings_Postfix(/*GameOptionsManager __instance*/)
    {
        Main.CurrentOptions.SetInt(Int32OptionNames.RulePreset, 100);
        Main.CurrentOptions.SetBool(BoolOptionNames.IsDefaults, false);

        foreach (var Option in OptionItem.AllTBROptions)
        {
            if (Option.TryCast<OptionCheckboxItem>(out var Bool))
            {
                if (Bool.VanillaOption != null)
                {
                    Bool.SyncAUOption();
                }
            }
            else if (Option.TryCast<OptionFloatItem>(out var Float))
            {
                if (Float.VanillaOption != null)
                {
                    Float.SyncAUOption();
                }
            }
            else if (Option.TryCast<OptionIntItem>(out var Int))
            {
                if (Int.VanillaOption != null)
                {
                    Int.SyncAUOption();
                }
            }
            else if (Option.TryCast<OptionStringItem>(out var String))
            {
                if (String.VanillaOption != null)
                {
                    String.SyncAUOption();
                }
            }
        }
    }
}

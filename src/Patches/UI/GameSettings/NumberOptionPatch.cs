using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.GameSettings;

[HarmonyPatch(typeof(NumberOption))]
internal class NumberOptionPatch
{
    [HarmonyPatch(nameof(NumberOption.Increase))]
    [HarmonyPrefix]
    private static bool Increase_Prefix(NumberOption __instance)
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (__instance.Value + __instance.Increment * times > __instance.ValidRange.max)
        {
            __instance.Value = __instance.ValidRange.max;
        }
        else
        {
            __instance.Value = __instance.ValidRange.Clamp(__instance.Value + __instance.Increment * times);
        }
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();
        return false;
    }

    [HarmonyPatch(nameof(NumberOption.Decrease))]
    [HarmonyPrefix]
    private static bool Decrease_Prefix(NumberOption __instance)
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (__instance.Value - __instance.Increment * times < __instance.ValidRange.min)
        {
            __instance.Value = __instance.ValidRange.min;
        }
        else
        {
            __instance.Value = __instance.ValidRange.Clamp(__instance.Value - __instance.Increment * times);
        }
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();
        return false;
    }
}

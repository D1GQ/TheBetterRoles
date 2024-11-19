using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ExileController))]
class ExileControllerPatch
{
    [HarmonyPatch(nameof(ExileController.HandleText))]
    [HarmonyPrefix]
    public static void HandleText_Prefix(ExileController __instance)
    {
        var init = __instance.initData;

        if (init != null && init.outfit != null)
        {
            if (VanillaGameSettings.ConfirmEjects.GetBool() || init.networkedPlayer.ExtendedData().RoleInfo.Role.AlwaysShowVoteOutMsg)
            {
                __instance.completeString = string.Format(Translator.GetString("ConfirmEject"), init.outfit.PlayerName, $"{Utils.GetCustomRoleNameAndColor(init.networkedPlayer.ExtendedData().RoleInfo.Role.RoleType)}");
            }
            else
            {
                __instance.completeString = string.Format(Translator.GetString(StringNames.ExileTextNonConfirm), init.outfit.PlayerName);
            }
        }

        __instance.ImpostorText.text = "";
    }

    [HarmonyPatch(nameof(ExileController.WrapUp))]
    [HarmonyPrefix]
    public static bool WrapUp_Prefix(ExileController __instance)
    {
        CustomRoleManager.RoleListenerOther(role => role.OnExileEnd(__instance?.initData?.networkedPlayer?.Object, __instance?.initData?.networkedPlayer));
        CustomRoleManager.RoleListener(PlayerControl.LocalPlayer, role => role.SetAllCooldowns());
        Utils.DirtyAllNames();

        if (__instance.initData.networkedPlayer != null)
        {
            PlayerControl @object = __instance.initData.networkedPlayer.Object;
            if (@object)
            {
                @object.Exiled();
            }
            __instance.initData.networkedPlayer.IsDead = true;
        }
        __instance.ReEnableGameplay();
        __instance.gameObject.DestroyObj();

        return false;
    }
}

using HarmonyLib;
using InnerNet;
using System.Reflection;

namespace TheBetterRoles.Patches;

class RolePatch
{
    [HarmonyPatch(typeof(HudManager))]
    public class HudManagerPatch
    {
        [HarmonyPatch(nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(/*HudManager __instance*/)
        {
            List<BaseButton> Remove = [];
            foreach (var button in BaseButton.AllButtons)
            {
                if (button.Button.gameObject != null && button != null)
                {
                    if (button.Role._player.AmOwner)
                    {
                        button.Update();
                    }
                }
                else
                {
                    Remove.Add(button);
                }
            }
            Remove.ToList().ForEach(b => BaseButton.AllButtons.Remove(b));
        }
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(PlayerControl __instance)
        {
            if (__instance?.BetterData()?.RoleInfo?.RoleAssigned == true)
            {
                __instance.BetterData().RoleInfo.Role.Update();
            }
        }
    }

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}

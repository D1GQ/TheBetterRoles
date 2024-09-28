using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(TaskAdderGame))]
class TaskAdderGamePatch
{
    public static Dictionary<TaskAddButton, CustomRoleBehavior> rolesForButtons = [];
    [HarmonyPatch(nameof(TaskAdderGame.ShowFolder))]
    [HarmonyPrefix]
    public static bool ShowFolder_Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        rolesForButtons.Clear();

        float num = 0f;
        float num2 = 0f;
        float num3 = 0f;
        for (int m = 0; m < CustomRoleManager.allRoles.Length; m++)
        {
            CustomRoleBehavior roleBehaviour = CustomRoleManager.allRoles[m];

            TaskAddButton taskAddButton = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
            taskAddButton.SafePositionWorld = __instance.SafePositionWorld;
            taskAddButton.Text.text = "Be_" + roleBehaviour.RoleName + ".exe";
            __instance.AddFileAsChild(__instance.Root, taskAddButton, ref num, ref num2, ref num3);

            taskAddButton.FileImage.color =Utils.GetCustomRoleColor(roleBehaviour.RoleType);
            taskAddButton.RolloverHandler.OverColor = Utils.GetCustomRoleColor(roleBehaviour.RoleType) + new Color(0.35f, 0.35f, 0.35f);
            taskAddButton.RolloverHandler.OutColor = Utils.GetCustomRoleColor(roleBehaviour.RoleType);
            taskAddButton.Button.OnClick.RemoveAllListeners();
            taskAddButton.Button.OnClick.AddListener((Action)(() =>
            {
                var player = PlayerControl.LocalPlayer;

                player.Revive();
                if (roleBehaviour.RoleTeam != CustomRoleTeam.None)
                {
                    CustomRoleManager.SetCustomRole(player, roleBehaviour.RoleType);
                }
                else
                {
                    if (!player.BetterData().RoleInfo.Addons.Contains(roleBehaviour))
                    {
                        CustomRoleManager.AddAddon(player, roleBehaviour.RoleType);
                    }
                    else
                    {
                        CustomRoleManager.RemoveAddon(player, roleBehaviour.RoleType);
                    }
                }

                foreach (var item in rolesForButtons.Keys)
                {
                    item.Overlay.enabled = PlayerControl.LocalPlayer.BetterData().RoleInfo.RoleType == rolesForButtons[item].RoleType;
                }
            }));
            UnityEngine.Object.Destroy(taskAddButton);
            rolesForButtons[taskAddButton] = roleBehaviour;
        }

        foreach (var item in rolesForButtons.Keys)
        {
            item.Overlay.enabled = PlayerControl.LocalPlayer.BetterData().RoleInfo.RoleType == rolesForButtons[item].RoleType;
        }

        return false;
    }
}
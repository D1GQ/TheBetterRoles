using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(TaskAdderGame))]
class TaskAdderGamePatch
{
    public static Scroller? Scroller;

    public static Dictionary<TaskAddButton, CustomRoleBehavior> rolesForButtons = [];
    [HarmonyPatch(nameof(TaskAdderGame.ShowFolder))]
    [HarmonyPrefix]
    public static bool ShowFolder_Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        rolesForButtons.Clear();

        float num = 0f;
        float num2 = 0f;
        float num3 = 0f;

        if (Scroller == null)
        {
            Scroller = __instance.gameObject.AddComponent<Scroller>();
            Scroller.Inner = __instance.TaskParent.transform;
            Scroller.MouseMustBeOverToScroll = true;
            var box = __instance.TaskParent.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(100f, 100f);
            Scroller.ClickMask = box;
            Scroller.ScrollWheelSpeed = 0.7f;
            Scroller.SetYBoundsMin(1.7f);
            Scroller.SetYBoundsMax(10f);
            Scroller.allowY = true;

            __instance.FolderBackButton.gameObject.SetActive(false);
            __instance.PathText.gameObject.SetActive(false);
            __instance.transform.Find("TitleText_TMP").gameObject.SetActive(false);
            __instance.transform.Find("HomeButton").gameObject.SetActive(false);
            __instance.transform.Find("Background").localScale = new Vector3(1f, 5f, 1f);
        }

        TaskAddButton ghostAddButton = UnityEngine.Object.Instantiate(__instance.RoleButton);
        ghostAddButton.SafePositionWorld = __instance.SafePositionWorld;
        ghostAddButton.Text.text = "Set_Dead\n.exe";
        __instance.AddFileAsChild(__instance.Root, ghostAddButton, ref num, ref num2, ref num3);
        ghostAddButton.FileImage.color = new Color(1f, 1f, 1f, 0.5f);
        ghostAddButton.RolloverHandler.OverColor = new Color(1f, 1f, 1f, 0.5f);
        ghostAddButton.RolloverHandler.OutColor = new Color(1f, 1f, 1f, 0.5f);
        ghostAddButton.Overlay.enabled = false;
        ghostAddButton.Button.OnClick = new();
        ghostAddButton.Button.OnClick.AddListener((Action)(() =>
        {
            var player = PlayerControl.LocalPlayer;
            player.Exiled();
        }));

        for (int m = 0; m < CustomRoleManager.allRoles.Length; m++)
        {
            CustomRoleBehavior roleBehaviour = CustomRoleManager.allRoles[m];

            TaskAddButton taskAddButton = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
            taskAddButton.SafePositionWorld = __instance.SafePositionWorld;
            taskAddButton.Text.enableWordWrapping = false;
            if (!roleBehaviour.IsAddon) taskAddButton.Text.text = roleBehaviour.RoleName + "\n.Role";
            else taskAddButton.Text.text = roleBehaviour.RoleName + "\n.Addon";
            __instance.AddFileAsChild(__instance.Root, taskAddButton, ref num, ref num2, ref num3);

            taskAddButton.FileImage.color = Utils.GetCustomRoleColor(roleBehaviour.RoleType);
            taskAddButton.RolloverHandler.OverColor = Utils.GetCustomRoleColor(roleBehaviour.RoleType) + new Color(0.35f, 0.35f, 0.35f);
            taskAddButton.RolloverHandler.OutColor = Utils.GetCustomRoleColor(roleBehaviour.RoleType);
            taskAddButton.Button.OnClick.RemoveAllListeners();
            taskAddButton.Button.OnClick.AddListener((Action)(() =>
            {
                var player = PlayerControl.LocalPlayer;

                player.CustomRevive();
                if (!roleBehaviour.IsAddon)
                {
                    CustomRoleManager.SetCustomRole(player, roleBehaviour.RoleType);
                }
                else
                {
                    if (!player.ExtendedData().RoleInfo.Addons.Any(r => r.RoleType == roleBehaviour.RoleType))
                    {
                        taskAddButton.Overlay.enabled = true;
                        CustomRoleManager.AddAddon(player, roleBehaviour.RoleType);
                    }
                    else
                    {
                        taskAddButton.Overlay.enabled = false;
                        CustomRoleManager.RemoveAddon(player, roleBehaviour.RoleType);
                    }
                }

                foreach (var items in rolesForButtons)
                {
                    if (items.Value.IsAddon) return;
                    items.Key.Overlay.enabled = PlayerControl.LocalPlayer.ExtendedData().RoleInfo.RoleType == rolesForButtons[items.Key].RoleType;
                }
            }));
            taskAddButton.DestroyMono();
            rolesForButtons[taskAddButton] = roleBehaviour;
        }

        foreach (var items in rolesForButtons)
        {
            items.Key.Overlay.enabled = PlayerControl.LocalPlayer.ExtendedData().RoleInfo.RoleType == rolesForButtons[items.Key].RoleType
                || PlayerControl.LocalPlayer.ExtendedData().RoleInfo.Addons.Any(r => r.RoleType == rolesForButtons[items.Key].RoleType);
        }

        return false;
    }
}
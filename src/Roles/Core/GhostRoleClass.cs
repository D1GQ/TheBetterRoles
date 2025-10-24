using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using UnityEngine;

namespace TheBetterRoles.Roles.Core;

internal abstract class GhostRoleClass : RoleClass
{
    protected override void SetUpSettings()
    {
        ResetOptionIDs();
        RoleOptions.RoleOptionItem = OptionPercentItem.Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        RoleOptions.RoleOptionItem.CreateDescriptionButton(Utils.GetCustomRoleInfo(RoleType, true));
        RoleOptions.AmountOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.Amount", (AmountSize, 15 / AmountSize * AmountSize, AmountSize), AmountSize, ("", ""), RoleOptions.RoleOptionItem);

        OptionItems.Initialize();

        if (TaskReliantRole)
        {
            RoleOptions.OverrideTasksOptionItem = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.OverrideTasks", false, RoleOptions.RoleOptionItem);
            RoleOptions.CommonTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.CommonTasks", (0, 10, 1), 2, ("", ""), RoleOptions.OverrideTasksOptionItem);
            RoleOptions.LongTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.LongTasks", (0, 10, 1), 2, ("", ""), RoleOptions.OverrideTasksOptionItem);
            RoleOptions.ShortTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.ShortTasks", (0, 10, 1), 4, ("", ""), RoleOptions.OverrideTasksOptionItem);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsGhostRolePatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPrefix]
        internal static bool HandleAnimation_Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] bool amDead)
        {
            var player = __instance.myPlayer;
            if (!player.IsGhostRole() || player?.IsAlive(true) != false) return true;

            Vector2 velocity = __instance.body.velocity;

            if (amDead)
            {
                __instance.myPlayer.cosmetics.SetGhost();
                if (__instance.myPlayer?.Role()?.IsGhostRole == true)
                {
                    if (!__instance.Animations.IsPlayingGuardianAngelIdleAnimation())
                    {
                        __instance.Animations.PlayGuardianAngelIdleAnimation();
                    }
                }
                else if (!__instance.Animations.IsPlayingGhostIdleAnimation())
                {
                    __instance.Animations.PlayGhostIdleAnimation();
                }

                __instance.myPlayer.SetHatAndVisorAlpha(0.5f);

                if (velocity.x < -0.01f) __instance.FlipX = true;
                else if (velocity.x > 0.01f) __instance.FlipX = false;
            }

            return false;
        }
    }
}

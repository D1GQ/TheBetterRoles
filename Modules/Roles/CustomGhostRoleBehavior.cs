using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Roles;

public abstract class CustomGhostRoleBehavior : CustomRoleBehavior
{
    public override bool IsGhostRole => true;
    private int tempBaseOptionNum = 0;
    private int GetBaseOptionID()
    {
        var num = tempBaseOptionNum;
        tempBaseOptionNum++;
        return RoleUID + num;
    }

    protected override void SetUpSettings()
    {
        tempBaseOptionNum = 0;
        RoleOptionItem = new BetterOptionPercentItem().Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        AmountOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.Amount"), [1, 15, 1], 1, "", "", RoleOptionItem);

        OptionItems.Initialize();

        if (TaskReliantRole)
        {
            OverrideTasksOptionItem = new BetterOptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.OverrideTasks"), false, RoleOptionItem);
            CommonTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.CommonTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            LongTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.LongTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            ShortTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.ShortTasks"), [0, 10, 1], 4, "", "", OverrideTasksOptionItem);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsGhostRolePatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPrefix]
        public static bool HandleAnimation_Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] bool amDead)
        {
            var player = __instance.myPlayer;
            if (player?.Role()?.IsGhostRole != true || player?.IsAlive(true) != false) return true;

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

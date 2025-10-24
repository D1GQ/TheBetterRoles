using HarmonyLib;
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Ghosts;

internal sealed class PhantomRoleTBR : GhostRoleClass, IRoleUpdateAction, IRoleMeetingAction, IRoleTaskAction, IRolePressAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 30;
    internal sealed override string RoleColorHex => "#A04D8A";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Phantom;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Ghost;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;
    internal sealed override bool CountToPlayerAmount => false;
    internal sealed override bool CanVent => _player.IsInVent();
    internal sealed override bool TaskReliantRole => true;
    internal sealed override bool HasSelfTask => !HasBeenClicked;

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    internal float Alpha = 0f;
    internal bool HasBeenClicked = false;

    internal sealed override void OnSetUpRole()
    {
        RoleButtons.VentButton.VisibleCondition = _player.IsInVent;
        RoleButtons.VentButton.Duration = 10f;
        RoleButtons.VentButton.UseAsDead = true;
        _player.ExtendedPC().InteractableTarget = false;
        _player.ClearAddons();
        _player.ExtendedData().PlayerVisionModPlus += 10;
        _player.CustomRevive(setReason: false);
        _player.ExtendedData().IsFakeDead = true;
        _player.cosmetics.GetPet()?.gameObject.SetActive(false);
        _player.cosmetics.SetPhantomRoleAlpha(0f);
        _player.SetCosmeticsActive(false);
        _player.SetPlayerTextActive(false);
        TryOverrideTasks(true);
        SpawnInRandomVent();
    }

    private void OnClick(bool set = true)
    {
        HasBeenClicked = set;
        _player.cosmetics.SetPhantomRoleAlpha(1f);
        _player.SetCosmeticsActive(true);
        _player.SetPlayerTextActive(true);
        _player.CustomExiled();
        _player.ExtendedPC().InteractableTarget = true;
        if (_player.IsLocalPlayer()) _player.ShieldBreakAnimation(RoleColorHex);
        _player.SetDeathReason(DeathReasons.Other, RoleColorHex, true);
    }

    internal sealed override void OnDeinitialize()
    {
        _player.ExtendedData().IsFakeDead = false;
        _player.ExtendedData().PlayerVisionModPlus -= 10;
        _player.cosmetics.GetPet()?.gameObject.SetActive(true);
        OnClick(false);
    }

    void IRoleTaskAction.TaskComplete(PlayerControl player, uint taskId)
    {
        CheckWinCondition();
    }

    void IRoleMeetingAction.MeetingBegin(MeetingHud meetingHud)
    {
        if (!HasBeenClicked)
        {
            _player.CustomExiled(setReason: false);
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (!HasBeenClicked)
        {
            _player.CustomRevive(setReason: false);
            SpawnInRandomVent();
        }
    }

    private void SpawnInRandomVent()
    {
        if (HasBeenClicked) return;
        var vent = _player.IsLocalPlayer() ? Main.AllEnabledVents[IRandom.Instance.Next(0, Main.AllEnabledVents.Length)] : Main.AllEnabledVents.First();

        if (vent != null)
        {
            _player.inVent = true;
            _player.Visible = false;
            _player.moveable = false;
            if (_player.IsLocalPlayer())
            {
                vent.TryMoveToVent(vent, out string _);
                vent.SetButtons(false);
                RoleButtons.VentButton?.SetDuration();
            }
        }
    }

    void IRoleUpdateAction.Update()
    {
        if (!HasBeenClicked)
        {
            _player.Visible = true;
        }
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (!HasBeenClicked)
        {
            if (_player.MyPhysics.Animations.Animator.GetCurrentAnimation() != _player.MyPhysics.Animations.group.IdleAnim)
            {
                Alpha += 0.005f;
            }
            else
            {
                Alpha -= 0.0038f;
            }

            Alpha = Math.Clamp(Alpha, 0f, 0.20f);

            _player.cosmetics.SetPhantomRoleAlpha(Alpha);
        }
    }

    void IRolePressAction.PlayerPress(PlayerControl player, PlayerControl target)
    {
        if (Alpha > 0.05f && target == _player && player != _player)
        {
            if (player.IsLocalPlayer() && player.IsAlive())
            {
                SendRoleSync(0);
                OnClick();
            }
        }
    }

    bool IRoleGameplayAction.WinCondition()
    {
        bool isWin = _player.Data.Tasks.ToArray().All(task => task.Complete) && _player.Data.Tasks.ToArray().Length > 0 && !HasBeenClicked;
        if (isWin)
        {
            _player.SendRpcRevive();
        }
        return isWin;
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    OnClick();
                }
                break;
        }
    }

    // Fix movement animation while technically dead
    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsPhantomPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPrefix]
        internal static bool HandleAnimation_Prefix(PlayerPhysics __instance)
        {
            if (!__instance.myPlayer.Is(RoleClassTypes.Phantom)) return true;

            Vector2 velocity = __instance.body.velocity;

            if (__instance.Animations.IsPlayingClimbAnimation())
            {
                return false;
            }

            if (velocity.sqrMagnitude >= 0.05f)
            {
                bool flipX = __instance.FlipX;
                if (velocity.x < -0.01f)
                {
                    __instance.FlipX = true;
                }
                else if (velocity.x > 0.01f)
                {
                    __instance.FlipX = false;
                }
                if (!__instance.Animations.IsPlayingRunAnimation() || flipX != __instance.FlipX || !__instance.myPlayer.cosmetics.IsSkinPlayingRunAnim())
                {
                    __instance.Animations.PlayRunAnimation();
                    __instance.myPlayer.cosmetics.AnimateSkinRun();
                    return false;
                }
            }
            else if (__instance.Animations.IsPlayingRunAnimation() || __instance.Animations.IsPlayingSpawnAnimation() || !__instance.Animations.IsPlayingSomeAnimation())
            {
                __instance.myPlayer.cosmetics.AnimateSkinIdle();
                __instance.Animations.PlayIdleAnimation();
            }

            return false;
        }
    }
}

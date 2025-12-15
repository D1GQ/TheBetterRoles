using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Ghosts;

internal sealed class PossessorRole : GhostRoleClass, IRoleUpdateAction, IRoleAbilityAction<PlayerControl>, IRoleMurderAction, IRoleReportAction, IRoleDeathAction, IRoleDisconnectAction
{
    internal sealed override int RoleId => 47;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Possessor;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Ghost;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;

    internal OptionItem? PossessCooldown;
    internal OptionItem? PossessDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                PossessCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Possessor.Option.PossessCooldown", (0f, 180f, 2.5f), 15f, ("", "s"), RoleOptions.RoleOptionItem),
                PossessDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Possessor.Option.PossessDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private static HashSet<PlayerControl> _possessed = [];
    private bool Possessing;
    private PlayerControl? PossessedTarget;
    private int PossessedOwnerId = -1;
    private PlayerAbilityButton? PossessButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            PossessButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Possessor.Ability.1"), PossessCooldown.GetFloat(), PossessDuration.GetFloat(), 0, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
            PossessButton.AddTargetCondition((target) =>
            {
                return !target.IsImpostorTeammate() && !_possessed.Contains(target);
            });
            PossessButton.DurationName = Translator.GetString("Role.Possessor.Ability.2");
            PossessButton.CanCancelDuration = true;
            PossessButton.UseAsDead = true;
            PossessButton.CheckCanBeInteracted = false;
        }
    }

    internal override void CleanUp()
    {
        _possessed = [];
    }

    void IRoleDisconnectAction.OnDisconnect(PlayerControl player, DisconnectReasons reason)
    {
        if (player == PossessedTarget)
        {
            ((IRoleAbilityAction)this).OnResetAbilityState(false);
        }
    }

    void IRoleDeathAction.OnDeathOther(PlayerControl player, DeathReasons reason)
    {
        if (player == PossessedTarget)
        {
            ((IRoleAbilityAction)this).OnResetAbilityState(false);
        }
    }

    bool IRoleReportAction.CheckBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? bodyData, bool isButton)
    {
        if (reporter == PossessedTarget && Possessing)
        {
            return false;
        }

        return true;
    }

    bool IRoleMurderAction.CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer == PossessedTarget && Possessing)
        {
            return false;
        }

        return true;
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (PossessedTarget != null && Possessing)
        {
            if (_player.IsLocalPlayer())
            {
                Vector2 vector = Vector2.zero;
                Vector2 Offset = PossessedTarget.MyPhysics.FlipX ? new Vector2(0.5f, 0.15f) : new Vector2(-0.5f, 0.15f);
                Vector2 vector2 = PossessedTarget.GetTruePosition() + Offset;
                Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
                Vector2 vector3 = PlayerControl.LocalPlayer.MyPhysics.GetVelocity() / PlayerControl.LocalPlayer.MyPhysics.TrueSpeed;
                Vector2 vector4 = vector2 - truePosition;
                float magnitude = vector4.magnitude;
                if (magnitude > 0.05f)
                {
                    vector4 = vector4.normalized * Mathf.Clamp(magnitude, 0.75f, 4f);
                    vector = vector3 * 0.8f + vector4 * 0.2f;
                }
                else
                {
                    vector *= 0.7f;
                }
                _player.MyPhysics.SetNormalizedVelocity(vector);
            }
            _player.MyPhysics.FlipX = _player.MyPhysics.body.position.x > PossessedTarget.MyPhysics.body.position.x;
        }
    }

    private void PossessPlayer(PlayerControl target)
    {
        if (_possessed.Contains(target)) return;
        if (Possessing) return;
        _possessed.Add(target);

        PossessedTarget = target;
        PossessedOwnerId = target.OwnerId;
        _player.MyPhysics.OwnerId = -1;
        target.MyPhysics.OwnerId = _player.OwnerId;
        target.NetTransform.OwnerId = _player.OwnerId;
        SetNetTransform(target.NetTransform);
        if (PlayerControl.LocalPlayer.IsImpostorTeammate())
        {
            target.SetTrueVisorColor(RoleColor);
        }
        _player.SetTrueVisorColor(RoleColor);
        if (_player.IsLocalPlayer() || PossessedTarget.IsLocalPlayer())
        {
            foreach (var con in Main.AllConsoles)
            {
                con.SetOutline(false, false);
            }
        }
        if (PossessedTarget.IsLocalPlayer())
        {
            Minigame.Instance?.Close();
        }
        if (_player.IsLocalPlayer())
        {
            _player.lightSource.transform.SetParent(target.transform);
            _player.lightSource.Initialize(target.Collider.offset * 0.5f);
            HudManager.Instance.PlayerCam.SetTarget(target);
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
            target.ShieldBreakAnimation(RoleColorHex);
        }
        Possessing = true;

        Networked.SendRoleSync(0, target);
    }

    private void UnPossessPlayer()
    {
        if (!Possessing) return;

        PossessedTarget.MyPhysics.OwnerId = PossessedOwnerId;
        PossessedTarget.NetTransform.OwnerId = PossessedOwnerId;
        _player.MyPhysics.OwnerId = _player.OwnerId;
        SetNetTransform(PossessedTarget.NetTransform);
        if (PlayerControl.LocalPlayer.IsImpostorTeammate())
        {
            PossessedTarget.SetTrueVisorColor(Palette.VisorColor);
        }
        _player.SetTrueVisorColor(Palette.VisorColor);
        if (_player.IsLocalPlayer() || PossessedTarget.IsLocalPlayer())
        {
            foreach (var con in Main.AllConsoles)
            {
                con.SetOutline(false, false);
            }
        }
        if (_player.IsLocalPlayer())
        {
            _player.lightSource.transform.SetParent(_player.transform);
            _player.lightSource.Initialize(_player.Collider.offset * 0.5f);
            HudManager.Instance.PlayerCam.SetTarget(_player);
            HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
            _player.ShieldBreakAnimation(RoleColorHex);
        }
        PossessedTarget = null;
        PossessedOwnerId = -1;
        Possessing = false;

        Networked.SendRoleSync(1);
    }

    private void SetNetTransform(CustomNetworkTransform transform)
    {
        transform.Halt();
        transform.lastSequenceId += 15;
        transform.lastPosition = PossessedTarget.MyPhysics.body.position;
        transform.lastPosSent = PossessedTarget.MyPhysics.body.position * 5;
    }

    internal sealed override bool HidePlayerInfoOther(PlayerControl target)
    {
        if (_player.IsLocalPlayer())
        {
            if (!target.IsImpostorTeammate())
            {
                return true;
            }
        }

        return false;
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null)
                    {
                        PossessPlayer(target);
                        PossessButton?.SetDuration();
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        _possessed.Remove(PossessedTarget);
        UnPossessPlayer();
        PossessButton?.SetCooldown(durationState: 0);
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                PossessPlayer(data.MessageReader.ReadFast<PlayerControl>());
                break;
            case 1:
                UnPossessPlayer();
                break;
        }
    }
}
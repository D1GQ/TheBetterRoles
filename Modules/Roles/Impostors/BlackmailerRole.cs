
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class BlackmailerRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 33;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Blackmailer;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? BlackmailCooldown;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                BlackmailCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Blackmailer.Option.BlackmailCooldown"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
            ];
        }
    }

    public PlayerAbilityButton? BlackmailButton;
    private NetworkedPlayerInfo? blackmailed;

    public override void OnSetUpRole()
    {
        BlackmailButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Blackmailer.Ability.1"), BlackmailCooldown.GetFloat(), 0, 1, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id) 
        {
            case 5:
                {
                    if (target != null)
                    {
                        blackmailed = target.Data;
                        target.DirtyName();
                    }
                }
                break;
        }
    }

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        if (blackmailed.IsLocalData() && blackmailed.IsAlive())
        {
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
            DestroyableSingleton<HudManager>.Instance.Chat.chatButton.enabled = false;
            DestroyableSingleton<HudManager>.Instance.Chat.chatButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (blackmailed.IsLocalData())
        {
            DestroyableSingleton<HudManager>.Instance.Chat.chatButton.enabled = true;
            DestroyableSingleton<HudManager>.Instance.Chat.chatButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
        }
        blackmailed?.Object?.DirtyName();
        blackmailed = null;
        BlackmailButton?.SetUses(1);
    }

    public override string SetNameMark(PlayerControl target)
    {
        if (blackmailed != null && (GameState.IsMeeting || _player.IsLocalPlayer()))
        {
            if (blackmailed == target.Data)
            {
                return $"<{RoleColor}>╳</color>";
            }
        }

        return string.Empty;
    }

    public override string AddMeetingText(ref CustomClip? clip)
    {
        var Blackmailed = blackmailed.Object;
        if (Blackmailed != null && Blackmailed.IsAlive())
        {
            clip = new() { Clip = DestroyableSingleton<MeetingIntroAnimation>.Instance.PlayerDeadSound };
            if (Blackmailed.IsLocalPlayer())
            {
                return $"<{RoleColor}>{Translator.GetString("Role.Blackmailer.YouMsg")}</color>";
            }
            else
            {
                return $"<{RoleColor}>{string.Format(Translator.GetString("Role.Blackmailer.AllMsg"), Blackmailed.GetPlayerNameAndColor())}</color>";
            }
        }

        return string.Empty;
    }
}

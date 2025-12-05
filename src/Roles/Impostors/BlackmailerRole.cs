using Hazel;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class BlackmailerRole : ImpostorRoleTBR, IRoleAbilityAction<PlayerControl>, IRoleMeetingAction
{
    internal sealed override int RoleId => 33;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Blackmailer;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;

    internal OptionItem? BlackmailCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                BlackmailCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Blackmailer.Option.BlackmailCooldown", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private NetworkedPlayerInfo? blackmailed;
    internal PlayerAbilityButton? BlackmailButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            BlackmailButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Blackmailer.Ability.1"), BlackmailCooldown.GetFloat(), 0, 1, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
        }
    }

    internal sealed override void OnDeinitialize()
    {
        TryRemoveBlackmail();
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    blackmailed = target.Data;
                    target.DirtyName();
                }
                break;
        }
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        if (blackmailed.IsLocalData() && blackmailed.IsAlive())
        {
            HudManager.Instance.Chat.ForceClosed();
            HudManager.Instance.Chat.chatButton.enabled = false;
            HudManager.Instance.Chat.chatButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        TryRemoveBlackmail();
    }

    private void TryRemoveBlackmail()
    {
        if (blackmailed != null)
        {
            if (blackmailed.IsLocalData())
            {
                HudManager.Instance.Chat.chatButton.enabled = true;
                HudManager.Instance.Chat.chatButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            }
            blackmailed = null;
            BlackmailButton?.SetUses(1);
        }
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (blackmailed != null && (GameState.IsMeeting || _player.IsLocalPlayer() || !localPlayer.IsAlive()))
        {
            if (blackmailed == target.Data)
            {
                return $"<{RoleColorHex}>╳</color>";
            }
        }

        return string.Empty;
    }

    string IRoleMeetingAction.AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = 100;
        var Blackmailed = blackmailed?.Object;
        if (Blackmailed != null && Blackmailed.IsAlive())
        {
            clip = new() { Clip = MeetingHud.Instance.MeetingIntro.PlayerDeadSound };
            if (Blackmailed.IsLocalPlayer())
            {
                return $"<{RoleColorHex}>{Translator.GetString("Role.Blackmailer.YouMsg")}</color>";
            }
            else
            {
                return $"<{RoleColorHex}>{Translator.GetString("Role.Blackmailer.AllMsg", [Blackmailed.GetPlayerNameAndColor()])}</color>";
            }
        }

        return string.Empty;
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WriteFast(blackmailed);
    }

    public override void Deserialize(MessageReader reader)
    {
        blackmailed = reader.ReadFast<NetworkedPlayerInfo>();
    }
}

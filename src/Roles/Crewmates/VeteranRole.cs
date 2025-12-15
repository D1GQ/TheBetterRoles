using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class VeteranRole : CrewmateRoleTBR, IRoleAbilityAction, IRoleMurderAction, IRoleTaskAction
{
    internal sealed override int RoleId => 16;
    internal sealed override string RoleColorHex => "#BB9B4F";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Veteran;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? CanBeKilledOnAlert;
    internal OptionItem? AlertCooldown;
    internal OptionItem? AlertDuration;
    internal OptionItem? MaximumNumberOfAlerts;
    internal OptionItem? AlertsGainFromTask;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CanBeKilledOnAlert = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Veteran.Option.CanBeKilledOnAlert", false, RoleOptions.RoleOptionItem),
                AlertCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Veteran.Option.AlertCooldown", (0f, 180f, 2.5f), 20, ("", "s"), RoleOptions.RoleOptionItem),
                AlertDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Veteran.Option.AlertDuration", (0f, 180f, 2.5f), 12, ("", "s"), RoleOptions.RoleOptionItem),
                MaximumNumberOfAlerts = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Veteran.Option.MaximumNumberOfAlerts", (1, 100, 1), 3, ("", ""), RoleOptions.RoleOptionItem),
                AlertsGainFromTask = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Veteran.Option.AlertsGainFromTask", (0f, 100f, 0.5f), 0.5f, ("", ""), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool OnAlert = false;
    internal BaseAbilityButton? AlertButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            AlertButton = RoleButtons.AddButton(BaseAbilityButton.Create(5, Translator.GetString("Role.Veteran.Ability.1"), AlertCooldown.GetFloat(), AlertDuration.GetFloat(), 1, null, this, true));
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                {
                    AlertButton?.SetDuration();
                    OnAlert = true;
                    MarkDirty();
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
        OnAlert = false;
        MarkDirty();
    }

    bool IRoleMurderAction.CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == _player)
        {
            if (OnAlert)
            {
                if (_player.IsLocalPlayer()) _player.SendRpcMurder(killer, true, true, MultiMurderFlags.playSound | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation);
                return CanBeKilledOnAlert.GetBool();
            }
        }

        return true;
    }

    void IRoleTaskAction.TaskComplete(PlayerControl player, uint taskId)
    {
        if (player.IsLocalPlayer())
        {
            AlertButton?.GainUse(AlertsGainFromTask.GetFloat(), MaximumNumberOfAlerts.GetInt());
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.Write(OnAlert);
    }

    public override void Deserialize(MessageReader reader)
    {
        OnAlert = reader.ReadBoolean();
    }
}

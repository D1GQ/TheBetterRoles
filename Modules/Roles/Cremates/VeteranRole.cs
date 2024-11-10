
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;

namespace TheBetterRoles.Roles;

public class VeteranRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 16;
    public override string RoleColor => "#BB9B4F";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Veteran;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? CanBeKilledOnAlert;
    public BetterOptionItem? AlertCooldown;
    public BetterOptionItem? AlertDuration;
    public BetterOptionItem? MaximumNumberOfAlerts;
    public BetterOptionItem? AlertsGainFromTask;

    public BaseAbilityButton? AlertButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CanBeKilledOnAlert = new BetterOptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Veteran.Option.CanBeKilledOnAlert"), false, RoleOptionItem),
                AlertCooldown = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertCooldown"), [0f, 180f, 2.5f], 20, "", "s", RoleOptionItem),
                AlertDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertDuration"), [0f, 180f, 2.5f], 12, "", "s", RoleOptionItem),
                MaximumNumberOfAlerts = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Veteran.Option.MaximumNumberOfAlerts"), [1, 100, 1], 3, "", "", RoleOptionItem),
                AlertsGainFromTask = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertsGainFromTask"), [0f, 100f, 0.5f], 1, "", "", RoleOptionItem),
            ];
        }
    }
    private bool OnAlert = false;

    public override void OnSetUpRole()
    {
        AlertButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Veteran.Ability.1"), AlertCooldown.GetFloat(), AlertDuration.GetFloat(), 1, null, this, true));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    AlertButton.SetDuration();
                    OnAlert = true;
                }
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    public override void OnResetAbilityState(bool isTimeOut)
    {
        OnAlert = false;
    }

    public override bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (OnAlert)
        {
            _player.SendRpcMurder(killer, true, MultiMurderFlags.playSound | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation);
            return CanBeKilledOnAlert.GetBool();
        }

        return true;
    }

    private float gainedUses = 0f;
    public override void OnTaskComplete(PlayerControl player, uint taskId)
    {
        if (_player.IsLocalPlayer())
        {
            int currentUses = AlertButton.Uses;
            gainedUses += AlertsGainFromTask.GetFloat();
            if (gainedUses % 1 != 0)
            {
                return;
            }
            int maxAlerts = MaximumNumberOfAlerts.GetInt();
            int newUses = Math.Clamp(currentUses + (int)gainedUses, 0, maxAlerts);
            AlertButton.SetUses(newUses);
            gainedUses = 0f;
        }
    }
}

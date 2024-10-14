
using Hazel;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class VeteranRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#007A00";
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

    public AbilityButton? AlertButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CanBeKilledOnAlert = new BetterOptionCheckboxItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.Veteran.Option.CanBeKilledOnAlert"), false, RoleOptionItem),
                AlertCooldown = new BetterOptionFloatItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertCooldown"), [0f, 180f, 2.5f], 20, "", "s", RoleOptionItem),
                AlertDuration = new BetterOptionFloatItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertDuration"), [0f, 180f, 2.5f], 12, "", "s", RoleOptionItem),
                MaximumNumberOfAlerts = new BetterOptionIntItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Veteran.Option.MaximumNumberOfAlerts"), [1, 100, 1], 3, "", "", RoleOptionItem),
                AlertsGainFromTask = new BetterOptionIntItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Veteran.Option.AlertsGainFromTask"), [0, 100, 1], 1, "", "", RoleOptionItem),
            ];
        }
    }
    private bool OnAlert = false;

    public override void OnSetUpRole()
    {
        AlertButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Veteran.Ability.1"), AlertCooldown.GetFloat(), AlertDuration.GetFloat(), 1, null, this, true));
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

    public override void OnResetAbilityState(bool isTimeOut = false)
    {
        OnAlert = false;
    }

    public override bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (OnAlert)
        {
            _player.MurderSync(killer, true, false, true, true, true);
            return CanBeKilledOnAlert.GetBool();
        }

        return true;
    }

    public override void OnTaskComplete(PlayerControl player, uint taskId)
    {
        int currentUses = AlertButton.Uses;
        int gainedAlerts = AlertsGainFromTask.GetInt();
        int maxAlerts = MaximumNumberOfAlerts.GetInt();
        int newUses = Math.Clamp(currentUses + gainedAlerts, 0, maxAlerts);
        AlertButton.SetUses(newUses);
    }
}

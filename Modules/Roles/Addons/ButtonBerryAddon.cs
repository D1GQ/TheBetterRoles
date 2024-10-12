
using Hazel;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class ButtonBerryAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#FF00EE";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.ButtonBerry;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.AbilityAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public AbilityButton? MeetingButton;
    public override void OnSetUpRole()
    {
        MeetingButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.ButtonBerry.Ability.1"), 0, 0, 1, null, this, false));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                _player.RemainingEmergencies++;
                _player.ReportBodySync(null);
                RemoveButton(MeetingButton);
                break;
        }
    }
}

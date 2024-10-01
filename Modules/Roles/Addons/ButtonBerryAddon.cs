
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

    public AbilityButton? MeetingButton = new();

    public override void SetUpRole()
    {
        OptionItems.Initialize();

        MeetingButton = AddButton(new AbilityButton().Create(105, Translator.GetString("Role.ButtonBerry.Ability.1"), 0, 0, 0, null, this, false)) as AbilityButton;
    }

    public override void OnAbilityUse(int id, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 105:
                _player.ReportBodySync(null);
                RemoveButton(MeetingButton);
                break;
        }

        base.OnAbilityUse(id, target, vent, body);
    }
}

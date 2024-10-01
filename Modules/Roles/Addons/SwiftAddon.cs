
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class SwiftAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#8DECFF";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Swift;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;

    public BetterOptionItem? SpeedX;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SpeedX = new BetterOptionFloatItem().Create(RoleId + 10, SettingsTab, Translator.GetString("Role.Swift.Option.Speed"), [1.5f, 5f, 0.25f], 2f, "x", "", RoleOptionItem),
            ];
        }
    }

    public AbilityButton? MeetingButton = new();
    public override void SetUpRole()
    {
        OptionItems.Initialize();

        _player.MyPhysics.Speed = PlayerSpeed * SpeedX.GetFloat();
    }

    public override void OnDeinitialize()
    {
        _player.MyPhysics.Speed = PlayerSpeed / SpeedX.GetFloat();
    }
}

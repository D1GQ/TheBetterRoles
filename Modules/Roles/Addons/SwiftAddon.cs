
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class SwiftAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 28;
    public override string RoleColor => "#8DECFF";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Swift;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override TBROptionTab? SettingsTab => BetterTabs.Addons;

    public TBROptionItem? SpeedX;
    private bool HasSpeed = false;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SpeedX = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Swift.Option.Speed"), [1.5f, 5f, 0.25f], 2f, "x", "", RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        SetSpeed();
    }

    public override void OnDeinitialize()
    {
        ResetSpeed();
    }

    public override void OnDisguise(PlayerControl player)
    {
        ResetSpeed();
    }

    public override void OnUndisguise(PlayerControl player)
    {
        SetSpeed();
    }

    private void SetSpeed()
    {
        if (HasSpeed) return;
        HasSpeed = true;
        _player.MyPhysics.Speed = PlayerSpeed * SpeedX.GetFloat();
    }
    private void ResetSpeed()
    {
        if (!HasSpeed) return;
        HasSpeed = false;
        _player.MyPhysics.Speed = PlayerSpeed / SpeedX.GetFloat();
    }
}

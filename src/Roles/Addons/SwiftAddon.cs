using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class SwiftAddon : AddonClass, IRoleDisguiseAction
{
    internal sealed override int RoleId => 28;
    internal sealed override string RoleColorHex => "#8DECFF";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Swift;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HelpfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal OptionItem? SpeedX;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SpeedX = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Swift.Option.Speed", (1.5f, 5f, 0.25f), 2f, ("", "x"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool HasSpeed = false;
    internal sealed override void OnSetUpRole()
    {
        SetSpeed();
    }

    internal sealed override void OnDeinitialize()
    {
        ResetSpeed();
    }

    void IRoleDisguiseAction.Disguise(PlayerControl player)
    {
        ResetSpeed();
    }

    void IRoleDisguiseAction.Undisguise(PlayerControl player)
    {
        SetSpeed();
    }

    private void SetSpeed()
    {
        if (HasSpeed) return;
        HasSpeed = true;
        _player.MyPhysics.Speed = _PlayerSpeed * SpeedX.GetFloat();
    }
    private void ResetSpeed()
    {
        if (!HasSpeed) return;
        HasSpeed = false;
        _player.MyPhysics.Speed = _PlayerSpeed / SpeedX.GetFloat();
    }
}

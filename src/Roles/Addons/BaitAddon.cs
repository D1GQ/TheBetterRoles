using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class BaitAddon : AddonClass, IRoleMurderAction
{
    internal sealed override int RoleId => 23;
    internal sealed override string RoleColorHex => "#00BDA4";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Bait;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.GeneralAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal OptionItem? Delay;
    internal OptionItem? MaximumDelay;
    internal OptionItem? MinimumDelay;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                Delay = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Bait.Option.Delay", false, RoleOptions.RoleOptionItem),
                MaximumDelay = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Bait.Option.MaximumDelay", (0, 10, 1), 5, ("", "s"), Delay),
                MinimumDelay = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Bait.Option.MinimumDelay", (0, 10, 1), 0, ("", "s"), Delay),
            ];
        }
    }

    void IRoleMurderAction.Murder(PlayerControl killer, PlayerControl target, bool suicide, bool IsAbility)
    {
        if (target == _player && !suicide && _player.IsLocalPlayer())
        {
            var num = Delay.GetBool() ? IRandom.Instance.Next(MinimumDelay.GetInt(), MaximumDelay.GetInt()) : 0;
            if (Delay.GetBool() && MinimumDelay.GetInt() >= MaximumDelay.GetInt() || MaximumDelay.GetInt() <= MinimumDelay.GetInt())
                num = 0;

            _ = new LateTask(() =>
            {
                CustomSoundsManager.Instance.Play(Sounds.Congrats, 3.5f);
                killer.SendRpcReportBody(_data);
            }, num + 1f, shouldLog: false);
        }
    }
}

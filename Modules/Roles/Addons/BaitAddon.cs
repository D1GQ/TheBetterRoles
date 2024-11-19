using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;

namespace TheBetterRoles.Roles;

public class BaitAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 23;
    public override string RoleColor => "#00BDA4";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Bait;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.GeneralAddon;
    public override TBROptionTab? SettingsTab => BetterTabs.Addons;
    public TBROptionItem? Delay;
    public TBROptionItem? MaximumDelay;
    public TBROptionItem? MinimumDelay;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                Delay = new TBROptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Bait.Option.Delay"), false, RoleOptionItem),
                MaximumDelay = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Bait.Option.MaximumDelay"), [0, 10, 1], 5, "", "s", Delay),
                MinimumDelay = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Bait.Option.MinimumDelay"), [0, 10, 1], 0, "", "s", Delay),
            ];
        }
    }

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player && !Suicide && _player.IsLocalPlayer())
        {
            var num = Delay.GetBool() ? IRandom.Instance.Next(MinimumDelay.GetInt(), MaximumDelay.GetInt()) : 0;
            if (Delay.GetBool() && MinimumDelay.GetInt() >= MaximumDelay.GetInt() || MaximumDelay.GetInt() <= MinimumDelay.GetInt())
                num = 0;

            _ = new LateTask(() =>
            {
                CustomSoundsManager.Play("Congrats", 3.5f);
                killer.SendRpcReportBody(_data);
            }, num + 1f, shouldLog: false);
        }
    }
}

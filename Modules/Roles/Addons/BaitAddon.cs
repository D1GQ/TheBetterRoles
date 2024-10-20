using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class BaitAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#00BDA4";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Bait;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.GeneralAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public BetterOptionItem? Delay;
    public BetterOptionItem? MaximumDelay;
    public BetterOptionItem? MinimumDelay;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                Delay = new BetterOptionCheckboxItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.Bait.Option.Delay"), false, RoleOptionItem),
                MaximumDelay = new BetterOptionIntItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Bait.Option.MaximumDelay"), [0, 10, 1], 5, "", "s", Delay),
                MinimumDelay = new BetterOptionIntItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Bait.Option.MinimumDelay"), [0, 10, 1], 0, "", "s", Delay),
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
                killer.ReportBodySync(_data);
            }, num + 1f, shoudLog: false);
        }
    }
}

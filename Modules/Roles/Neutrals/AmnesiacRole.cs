
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class AmnesiacRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 31;
    public override string RoleColor => "#96E5FF";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Amnesiac;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Benign;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? RememberCooldown;
    public DeadBodyAbilityButton? RememberButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RememberCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Amnesiac.Option.RememberCooldown"), [0f, 180f, 2.5f], 15, "", "s", RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        RememberButton = AddButton(new DeadBodyAbilityButton().Create(5, Translator.GetString("Role.Amnesiac.Ability.1"), RememberCooldown.GetFloat(), 0, 1, null, this, true, 0f));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (_player.IsLocalPlayer())
                    {
                        if (body != null)
                        {
                            var data = Utils.PlayerDataFromPlayerId(body.ParentId);
                            if (data != null)
                            {
                                if (_player.IsLocalPlayer()) Utils.FlashScreen(RoleColor);

                                _player.SendRpcSetCustomRole(data.BetterData().RoleInfo.RoleTypeWhenAlive);

                                /*
                                _player.ClearAddonsSync();
                                foreach (var addon in data.BetterData().RoleInfo.Addons)
                                {
                                    _player.SetRoleSync(addon.RoleType);
                                }
                                */
                            }
                        }
                    }
                }
                break;
        }
    }
}

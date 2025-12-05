using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class AmnesiacRole : RoleClass, IRoleAbilityAction<DeadBody>, IRoleDeathAction
{
    internal sealed override int RoleId => 31;
    internal sealed override string RoleColorHex => "#96E5FF";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Amnesiac;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Benign;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? RememberCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RememberCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Amnesiac.Option.RememberCooldown", (0f, 180f, 2.5f), 15, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    internal DeadBodyAbilityButton? RememberButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            RememberButton = RoleButtons.AddButton(DeadBodyAbilityButton.Create(5, Translator.GetString("Role.Amnesiac.Ability.1"), RememberCooldown.GetFloat(), 0, 1, null, this, true, 0f));
        }
    }

    void IRoleAbilityAction<DeadBody>.OnAbility(int id, DeadBody target)
    {
        switch (id)
        {
            case 5:
                {
                    RememberRole(target);
                }
                break;
        }
    }

    private readonly Dictionary<int, RoleClassTypes> rolesOnDeath = [];
    void IRoleDeathAction.OnDeathOther(PlayerControl player, DeathReasons reason)
    {
        rolesOnDeath[player.Data.PlayerId] = player.Role()?.RoleType ?? RoleClassTypes.Crewmate;
    }

    private void RememberRole(DeadBody body)
    {
        var data = Utils.PlayerDataFromPlayerId(body.ParentId);
        if (data != null)
        {
            if (_player.IsLocalPlayer())
            {
                var roleType = rolesOnDeath.ContainsKey(data.PlayerId) ? rolesOnDeath[data.PlayerId] : RoleClassTypes.Amnesiac;
                Utils.FlashScreen("amnesiac", RoleColorHex);
                var role = _player.SendRpcSetCustomRole(roleType);
                role.SetAllCooldownsHalf();
            }
        }
    }
}

using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class SheriffRole : CrewmateRoleTBR, IRoleAbilityAction<PlayerControl>
{
    internal sealed override int RoleId => 12;
    internal sealed override string RoleColorHex => "#feab00";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Sheriff;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? ShootCooldown;
    internal OptionItem? ShootDistance;
    internal OptionItem? ShotsAmount;
    internal OptionItem? Misfire;
    internal OptionItem? CanShootImpostors;
    internal OptionItem? CanShootNeutralsKilling;
    internal OptionItem? CanShootNeutralsBenign;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShootCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.ShootCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                ShootDistance = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.ShootDistance",
                ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 1, RoleOptions.RoleOptionItem),
                ShotsAmount = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.ShootsAmount", (0, 15, 1), 0, ("", ""), RoleOptions.RoleOptionItem, canBeInfinite: true),
                Misfire = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.KillOnMisfire",
                [Translator.GetString("Role.Option.AtSelf"), "Role.Option.AtTarget", "Role.Option.AtNone"], 0, RoleOptions.RoleOptionItem),
                CanShootImpostors = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.CanShootImpostors", true, RoleOptions.RoleOptionItem),
                CanShootNeutralsKilling = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.CanShootNeutralKilling", false, RoleOptions.RoleOptionItem),
                CanShootNeutralsBenign = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Sheriff.Option.CanShootNeutralBenign", false, RoleOptions.RoleOptionItem)
            ];
        }
    }

    internal PlayerAbilityButton? ShootButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            ShootButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Sheriff.Ability.1"), ShootCooldown.GetFloat(), 0, ShotsAmount.GetInt(), LoadAbilitySprite("Shoot"), this, true, ShootDistance.GetStringValue()));
            RoleButtons.RemoveButton(RoleButtons.KillButton);
            RoleButtons.KillButton = ShootButton;
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    ShootTarget(target);
                }
                break;
        }
    }

    private void ShootTarget(PlayerControl target)
    {
        if (_player.IsLocalPlayer())
        {
            if (target.Is(RoleClassTeam.Impostor) && CanShootImpostors.GetBool() ||
            target.Is(RoleClassTeam.Neutral) && target.Role().IsKillingRole && CanShootNeutralsKilling.GetBool() ||
            target.Is(RoleClassTeam.Neutral) && !target.Role().IsKillingRole && CanShootNeutralsBenign.GetBool() ||
            Misfire.GetStringValue() == 1)
            {
                _player.SendRpcMurder(target, true, true, MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
            }
            else if (Misfire.GetStringValue() == 0)
            {
                _player.SendRpcMurder(_player, true, true, MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
                _player.SetDeathReason(DeathReasons.Misfire, RoleColorHex, true);
            }
        }
    }
}

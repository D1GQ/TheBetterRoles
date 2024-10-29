
using Hazel;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class SheriffRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 12;
    public override string RoleColor => "#feab00";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Sheriff;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? ShootCooldown;
    public BetterOptionItem? ShootDistance;
    public BetterOptionItem? ShotsAmount;
    public BetterOptionItem? Misfire;
    public BetterOptionItem? CanShootImposters;
    public BetterOptionItem? CanShootNeutrals;
    public PlayerAbilityButton? ShootButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShootCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
                ShootDistance = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootDistance"),
                [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),
                ShotsAmount = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootsAmount"), [0, 15, 1], 0, "", "", RoleOptionItem),
                Misfire = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.KillOnMisfire"),
                [Translator.GetString("Role.Option.AtSelf"), Translator.GetString("Role.Option.AtTarget"), Translator.GetString("Role.Option.AtNone")], 0, RoleOptionItem),
                CanShootImposters = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootImpostors"), true, RoleOptionItem),
                CanShootNeutrals = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootNeutralKilling"), false, RoleOptionItem)
            ];
        }
    }
    public override void OnSetUpRole()
    {
        ShootButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Sheriff.Ability.1"), ShootCooldown.GetFloat(), ShotsAmount.GetInt(), LoadAbilitySprite("Shoot"), this, true, ShootDistance.GetValue()));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
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
        if (target.Is(CustomRoleTeam.Impostor) && CanShootImposters.GetBool() ||
            target.Is(CustomRoleTeam.Neutral) && CanShootNeutrals.GetBool() ||
            Misfire.GetValue() == 1)
        {
            if (_player.IsLocalPlayer())
            {
                _player.MurderSync(target, true, MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
            }
        }
        else if (Misfire.GetValue() == 0)
        {
            if (_player.IsLocalPlayer())
            {
                _player.MurderSync(_player, true, MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
            }
        }
    }
}

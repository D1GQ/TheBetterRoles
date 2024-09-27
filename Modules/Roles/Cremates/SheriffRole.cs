
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class SheriffRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 300;
    public override string RoleColor => "#feab00";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Sheriff;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Cremate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? ShootCooldown;
    public BetterOptionItem? ShootDistance;
    public BetterOptionItem? ShotsAmount;
    public BetterOptionItem? Misfire;
    public BetterOptionItem? CanShootImposters;
    public BetterOptionItem? CanShootNeutrals;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShootCooldown = new BetterOptionFloatItem().Create(RoleId + 10, SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
                ShootDistance = new BetterOptionStringItem().Create(RoleId + 15, SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootDistance"),
                [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),
                ShotsAmount = new BetterOptionIntItem().Create(RoleId + 20, SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootsAmount"), [0, 15, 1], 0, "", "", RoleOptionItem),
                Misfire = new BetterOptionStringItem().Create(RoleId + 25, SettingsTab, Translator.GetString("Role.Sheriff.Option.KillOnMisfire"),
                [Translator.GetString("Role.Option.AtSelf"), Translator.GetString("Role.Option.AtTarget"), Translator.GetString("Role.Option.AtNone")], 0, RoleOptionItem),
                CanShootImposters = new BetterOptionCheckboxItem().Create(RoleId + 30, SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootImposters"), true, RoleOptionItem),
                CanShootNeutrals = new BetterOptionCheckboxItem().Create(RoleId + 35, SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootNeutralKilling"), false, RoleOptionItem)
            ];
        }
    }
    public override void SetUpRole()
    {
        base.SetUpRole();
        OptionItems.Initialize();
        AddButton(new TargetButton().Create(5, Translator.GetString("Role.Sheriff.Ability.1"), ShootCooldown.GetFloat(), ShotsAmount.GetInt(), null, Role, true, ShootDistance.GetValue() + 1 / 1.5f));
    }

    public override void OnAbilityUse(int id, PlayerControl? target, Vent? vent)
    {
        switch (id)
        {
            case 5:
                {
                    ShotTarget(target);
                }
                break;
        }

        base.OnAbilityUse(id, target, vent);
    }

    public void ShotTarget(PlayerControl target)
    {
        if (target.Is(CustomRoleTeam.Impostor) && CanShootImposters.GetBool() ||
            target.Is(CustomRoleTeam.Neutral) && CanShootNeutrals.GetBool() ||
            Misfire.GetValue() == 1)
        {
            _player.MurderAction(target);
        }
        else if (Misfire.GetValue() == 0)
        {
            _player.MurderPlayer(_player, MurderResultFlags.Succeeded);
        }
    }
}

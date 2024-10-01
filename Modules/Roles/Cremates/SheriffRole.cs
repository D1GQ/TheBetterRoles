
using TheBetterRoles.Patches;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TheBetterRoles;

public class SheriffRole : CustomRoleBehavior
{
    // Role Info
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
    public TargetButton? ShootButton;
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
        ShootButton = AddButton(new TargetButton().Create(5, Translator.GetString("Role.Sheriff.Ability.1"), ShootCooldown.GetFloat(), ShotsAmount.GetInt(), LoadAbilitySprite("Shoot"), this, true, ShootDistance.GetValue() + 1 / 1.5f)) as TargetButton;
    }

    public override void OnAbilityUse(int id, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    ShootTarget(target);
                }
                break;
        }

        base.OnAbilityUse(id, target, vent, body);
    }

    private void ShootTarget(PlayerControl target)
    {
        if (target.Is(CustomRoleTeam.Impostor) && CanShootImposters.GetBool() ||
            target.Is(CustomRoleTeam.Neutral) && CanShootNeutrals.GetBool() ||
            Misfire.GetValue() == 1)
        {
            if (_player.IsLocalPlayer())
            {
                _player.MurderSync(target, true);
            }
        }
        else if (Misfire.GetValue() == 0)
        {
            if (_player.IsLocalPlayer())
            {
                _player.MurderSync(_player, true);
            }
        }
    }
}

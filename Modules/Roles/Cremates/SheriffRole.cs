
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;

namespace TheBetterRoles.Roles;

public class SheriffRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 12;
    public override string RoleColor => "#feab00";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Sheriff;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public TBROptionItem? ShootCooldown;
    public TBROptionItem? ShootDistance;
    public TBROptionItem? ShotsAmount;
    public TBROptionItem? Misfire;
    public TBROptionItem? CanShootImposters;
    public TBROptionItem? CanShootNeutrals;
    public PlayerAbilityButton? ShootButton;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShootCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
                ShootDistance = new TBROptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootDistance"),
                [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),
                ShotsAmount = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.ShootsAmount"), [0, 15, 1], 0, "", "", RoleOptionItem),
                Misfire = new TBROptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.KillOnMisfire"),
                [Translator.GetString("Role.Option.AtSelf"), Translator.GetString("Role.Option.AtTarget"), Translator.GetString("Role.Option.AtNone")], 0, RoleOptionItem),
                CanShootImposters = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootImpostors"), true, RoleOptionItem),
                CanShootNeutrals = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Sheriff.Option.CanShootNeutralKilling"), false, RoleOptionItem)
            ];
        }
    }
    public override void OnSetUpRole()
    {
        ShootButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Sheriff.Ability.1"), ShootCooldown.GetFloat(), 0, ShotsAmount.GetInt(), LoadAbilitySprite("Shoot"), this, true, ShootDistance.GetValue()));
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
                _player.SendRpcMurder(target, true, MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
            }
        }
        else if (Misfire.GetValue() == 0)
        {
            if (_player.IsLocalPlayer())
            {
                _player.SendRpcMurder(_player, true, MultiMurderFlags.spawnBody | MultiMurderFlags.playSound | MultiMurderFlags.showAnimation);
                _player.SetDeathReason(DeathReasons.Misfire, RoleColor, true);
            }
        }
    }
}

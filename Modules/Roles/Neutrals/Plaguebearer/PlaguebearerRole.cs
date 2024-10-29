
using Hazel;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class PlaguebearerRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 17;
    public override string RoleColor => "#97BD3D";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Plaguebearer;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Chaos;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? InfectCooldown;
    public BetterOptionItem? InfectDistance;
    public BetterOptionItem? PestilenceKillCooldown;
    public PlayerAbilityButton? InfectButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InfectCooldown = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.InfectCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
                InfectDistance = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.InfectDistance"),
                    [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),
                PestilenceKillCooldown = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.PestilenceKillCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        InfectButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Plaguebearer.Ability.1"), InfectCooldown.GetFloat(), 0, null, this, true, InfectDistance.GetValue()));
        InfectButton.TargetCondition = (PlayerControl target) =>
        {
            return !infected.Contains(target.Data);
        };
    }

    private List<NetworkedPlayerInfo> infected = [];
    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null)
                    {
                        InfectPlayer(target);
                    }
                }
                break;
        }
    }

    public override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var data in infected)
            {
                var player = data.Object;
                if (player != null)
                {
                    player.SetTrueVisorColor(Palette.VisorColor);
                    player.BetterData().NameColor = string.Empty;
                }
            }
        }
    }

    // Infact player on interactions
    public override void OnPlayerInteractedOther(PlayerControl player, PlayerControl target)
    {
        if (target == _player && !infected.Contains(player.Data))
        {
            InfectPlayer(player);
        }
        else if (infected.Contains(player.Data) && !infected.Contains(target.Data))
        {
            InfectPlayer(target);
        }
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        CheckPestillenceCondition();
    }

    private void InfectPlayer(PlayerControl player)
    {
        infected.Add(player.Data);
        if (_player.IsLocalPlayer())
        {
            player.SetTrueVisorColor(RoleColor32);
            player.BetterData().NameColor = RoleColor;
        }

        CheckPestillenceCondition();
    }

    private void CheckPestillenceCondition()
    {
        if (Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(infected.Contains))
        {
            CustomRoleManager.SetCustomRole(_player, CustomRoles.Pestillence);
            if (_player.Role() is PestillenceRole role)
            {
                role.WasTransformed = true;
            }
        }
    }
}
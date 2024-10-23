using Hazel;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class TransporterRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#68b2bf";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Transporter;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? TransportCooldown;
    public BetterOptionItem? MaximumNumberOfTransports;
    public BetterOptionItem? TransportsGainFromTask;

    public AbilityButton? TransportButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                TransportCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Transporter.Option.TransportCooldown"), [0f, 180f, 2.5f], 15, "", "s", RoleOptionItem),
                MaximumNumberOfTransports = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Transporter.Option.MaximumNumberOfTransports"), [1, 100, 1], 5, "", "", RoleOptionItem),
                TransportsGainFromTask = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Transporter.Option.TransportsGainFromTask"), [0, 100, 1], 1, "", "", RoleOptionItem),
            ];
        }
    }

    private PlayerMenu? menu;
    private PlayerControl? firstTarget;

    public override void OnSetUpRole()
    {
        TransportButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Transporter.Ability.1"), TransportCooldown.GetFloat(), 0, 1, null, this, true));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    TransportButton.AddUse();
                    TransportButton.SetCooldown(0);
                    if (_player.IsLocalPlayer())
                        menu = new PlayerMenu().Create(id, this, false, false, true);
                }
                break;
        }
    }

    public override void OnPlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close)
    {
        switch (id)
        {
            case 5:
                {
                    if (close)
                    {
                        firstTarget = null;
                        break;
                    }

                    if (playerPanel != null)
                    {
                        playerPanel.Background.color = new(1f, 1f, 1f, 0.5f);
                        playerPanel.Button.enabled = false;
                    }

                    if (firstTarget == null)
                    {
                        firstTarget = target;
                    }
                    else
                    {
                        TryToTransport(firstTarget, target);
                        this.menu.PlayerMinigame.Close();
                    }
                }
                break;
        }
    }

    private void TryToTransport(PlayerControl target1, PlayerControl target2)
    {
        if (target1.CanBeTeleported() && target2.CanBeTeleported() && target1 != null && target2 != null)
        {
            if (target1.IsLocalPlayer() || target2.IsLocalPlayer())
            {
                CustomSoundsManager.Play("Teleport", 2f);
                Utils.FlashScreen(RoleColor);
            }
            else if (_player.IsLocalPlayer())
            {
                Utils.FlashScreen(RoleColor);
            }

            var pos1 = target1.GetCustomPosition();
            var pos2 = target2.GetCustomPosition();

            target1.NetTransform.SnapTo(pos2);
            target2.NetTransform.SnapTo(pos1);

            TransportButton.RemoveUse();
            TransportButton.SetCooldown();
        }
        else
        {
            if (_player.IsLocalPlayer())
            {
                _player.ShieldBreakAnimation(RoleColor);
            }
        }

        firstTarget = null;
    }

    public override void OnTaskComplete(PlayerControl player, uint taskId)
    {
        int currentUses = TransportButton.Uses;
        int gainedUses = TransportsGainFromTask.GetInt();
        int maxAlerts = MaximumNumberOfTransports.GetInt();
        int newUses = Math.Clamp(currentUses + gainedUses, 0, maxAlerts);
        TransportButton.SetUses(newUses);
    }

}

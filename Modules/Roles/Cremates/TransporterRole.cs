using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class TransporterRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 15;
    public override string RoleColor => "#68b2bf";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Transporter;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? TransportCooldown;
    public BetterOptionItem? MaximumNumberOfTransports;
    public BetterOptionItem? TransportsGainFromTask;

    public BaseAbilityButton? TransportButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                TransportCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Transporter.Option.TransportCooldown"), [0f, 180f, 2.5f], 15, "", "s", RoleOptionItem),
                MaximumNumberOfTransports = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Transporter.Option.MaximumNumberOfTransports"), [1, 100, 1], 5, "", "", RoleOptionItem),
                TransportsGainFromTask = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Transporter.Option.TransportsGainFromTask"), [0f, 100f, 0.5f], 1f, "", "", RoleOptionItem),
            ];
        }
    }

    private PlayerMenu? menu;
    private PlayerControl? firstTarget;

    public override void OnSetUpRole()
    {
        TransportButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Transporter.Ability.1"), TransportCooldown.GetFloat(), 0, 1, null, this, true));
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
                    }

                    if (firstTarget == null)
                    {
                        firstTarget = target;
                    }
                    else if (firstTarget == target)
                    {
                        firstTarget = null;
                        if (playerPanel != null)
                        {
                            playerPanel.Background.color = new(1f, 1f, 1f, 1f);
                        }
                    }
                    else
                    {
                        TryToTransport(firstTarget, target);
                        menu?.PlayerMinigame?.Close();
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

    private float gainedUses = 0f;
    public override void OnTaskComplete(PlayerControl player, uint taskId)
    {
        int currentUses = TransportButton.Uses;
        gainedUses += TransportsGainFromTask.GetFloat();
        if (gainedUses % 1 != 0)
        {
            return;
        }
        int maxTransports = MaximumNumberOfTransports.GetInt();
        int newUses = Math.Clamp(currentUses + (int)gainedUses, 0, maxTransports);
        TransportButton.SetUses(newUses);
        gainedUses = 0f;
    }

}

using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class TransporterRole : CrewmateRoleTBR, IRoleAbilityAction, IRoleTaskAction, IRoleMenuAction
{
    internal sealed override int RoleId => 15;
    internal sealed override string RoleColorHex => "#68b2bf";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Transporter;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? TransportCooldown;
    internal OptionItem? MaximumNumberOfTransports;
    internal OptionItem? TransportsGainFromTask;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                TransportCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Transporter.Option.TransportCooldown", (0f, 180f, 2.5f), 15, ("", "s"), RoleOptions.RoleOptionItem),
                MaximumNumberOfTransports = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Transporter.Option.MaximumNumberOfTransports", (1, 100, 1), 5, ("", ""), RoleOptions.RoleOptionItem),
                TransportsGainFromTask = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Transporter.Option.TransportsGainFromTask", (0f, 100f, 0.5f), 1f, ("", ""), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private PlayerMenu? menu;
    private PlayerControl? firstTarget;
    internal BaseAbilityButton? TransportButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            TransportButton = RoleButtons.AddButton(BaseAbilityButton.Create(5, Translator.GetString("Role.Transporter.Ability.1"), TransportCooldown.GetFloat(), 0, 1, null, this, true));
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                {
                    TransportButton?.AddUse();
                    TransportButton?.SetCooldown(0);
                    menu = new PlayerMenu().Create(id, this, true, false, true);
                }
                break;
        }
    }

    void IRoleMenuAction.PlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close)
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

                    playerPanel.Background.color = new(1f, 1f, 1f, 0.5f);

                    if (firstTarget == null)
                    {
                        firstTarget = target;
                    }
                    else if (firstTarget == target)
                    {
                        firstTarget = null;
                        playerPanel.Background.color = new(1f, 1f, 1f, 1f);
                    }
                    else
                    {
                        TryToTransport(firstTarget, target);
                        Networked.SendRoleSync(firstTarget, target);
                        firstTarget = null;
                        menu?.PlayerMinigame?.Close();
                    }
                }
                break;
        }
    }

    private void TryToTransport(PlayerControl target1, PlayerControl target2)
    {
        if (target1 == null || target2 == null) return;

        if (target1.CanBeTeleported() && target2.CanBeTeleported())
        {
            if (target1.IsLocalPlayer() || target2.IsLocalPlayer())
            {
                CustomSoundsManager.Instance.Play(Sounds.Teleport, 2f);
                Utils.FlashScreen("transporter", RoleColorHex);
            }
            else if (_player.IsLocalPlayer())
            {
                Utils.FlashScreen("transporter", RoleColorHex);
            }

            var pos1 = target1.GetCustomPosition();
            var pos2 = target2.GetCustomPosition();

            target1.NetTransform.SnapTo(pos2);
            target2.NetTransform.SnapTo(pos1);

            TransportButton?.RemoveUse();
            TransportButton?.SetCooldown();
        }
        else
        {
            if (_player.IsLocalPlayer())
            {
                _player.ShieldBreakAnimation(RoleColorHex);
            }
        }
    }

    void IRoleTaskAction.TaskComplete(PlayerControl player, uint taskId)
    {
        if (player.IsLocalPlayer())
        {
            TransportButton?.GainUse(TransportsGainFromTask.GetFloat(), MaximumNumberOfTransports.GetInt());
        }
    }

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        TryToTransport(data.MessageReader.ReadFast<PlayerControl>(), data.MessageReader.ReadFast<PlayerControl>());
    }
}

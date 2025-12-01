using Hazel;
using TheBetterRoles.Data;
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

internal sealed class ConvictorRole : RoleClass, IRoleAbilityAction<PlayerControl>, IRoleMeetingAction, IRoleGuessAction, IRoleReportAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 53;
    internal sealed override string RoleColorHex => "#007DFF";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Convictor;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Evil;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? ConvictCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ConvictCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Convictor.Option.ConvictCooldown", (0f, 180f, 2.5f), 15f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool wasReported;
    private bool wasExiled;
    internal NetworkedPlayerInfo? Convicted;
    internal PlayerAbilityButton? ConvictButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            ConvictButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Convictor.Ability.1"), ConvictCooldown.GetFloat(), 0, 1, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    ConvictPlayer(target);
                    SendRoleSync(target);
                }
                break;
        }
    }

    private void ConvictPlayer(PlayerControl player)
    {
        Convicted = player.Data;
        if (_player.IsLocalPlayer())
        {
            player.SendRpcMurder(_player, true, false, MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.playSound);
        }
        RoleListener.InvokeRoles(role => role.SetAllCooldownsHalf(), player: player);
    }

    void IRoleReportAction.BodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton)
    {
        if (Convicted != null && body == _player.Data)
        {
            wasReported = true;
        }
        else
        {
            Convicted = null;
        }
    }

    void IRoleGuessAction.GuessOther(PlayerControl guesser, PlayerControl target, RoleClassTypes role)
    {
        if (wasReported && Convicted == target)
        {
            wasExiled = true;
            CheckWinCondition();
        }
        wasReported = false;
        Convicted = null;
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (wasReported && Convicted == exiled)
        {
            wasExiled = true;
            CheckWinCondition();
        }
        wasReported = false;
        Convicted = null;
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (target.Data == Convicted && wasReported && GameState.IsMeeting)
        {
            return "〤".ToColor(RoleColorHex);
        }

        return string.Empty;
    }

    bool IRoleGameplayAction.WinCondition() => wasExiled;

    internal override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        ConvictPlayer(reader.ReadFast<PlayerControl>());
    }
}
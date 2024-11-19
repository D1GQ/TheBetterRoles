using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.RPCs;

namespace TheBetterRoles.Commands;

public class SetRoleCommand : BaseCommand
{
    public override CommandType Type => CommandType.Sponsor;
    public override string Name => "up";
    public override string Description => "Set a Role, Ghost Role, or Add-on for the next game";

    public SetRoleCommand()
    {
        _arguments = new Lazy<BaseArgument[]>(() => new BaseArgument[]
        {
            new StringArgument(this),
        });
        roleArgument.suggestion = "{Name}";
    }
    private readonly Lazy<BaseArgument[]> _arguments;
    public override BaseArgument[]? Arguments => _arguments.Value;

    private StringArgument? roleArgument => (StringArgument)Arguments[0];

    public override bool ShowCommand() => GameState.IsHost && Main.MyData.IsSponsorTier3() || Main.MyData.HasAll() && Main.MyData.IsVerified();

    public override void Run()
    {
        if (GameState.IsHost || GameState.IsDev)
        {
            var role = CustomRoleManager.allRoles.FirstOrDefault(role => role.RoleName.StartsWith(roleArgument.Arg, StringComparison.OrdinalIgnoreCase));
            if (role != null)
            {
                if (GameState.IsHost && Main.MyData.IsSponsorTier3())
                {
                    QueueRoleAsHost(PlayerControl.LocalPlayer, role.RoleType);
                }
                else if (Main.MyData.HasAll() && Main.MyData.IsVerified())
                {
                    Rpc<RpcQueueRole>.Instance.SendTo(GameData.Instance.GetHost().ClientId, new(role.RoleType));
                }
            }
            else
            {
                CommandErrorText("Unable to find role");
            }
        }
    }

    public static void RequestQueueRole(PlayerControl player, CustomRoleType roleType)
    {
        var role = Utils.GetCustomRoleClass(roleType);
        if (player.ExtendedData().MyUserData.HasAll() && Main.MyData.IsVerified(player) && GameState.IsHost)
        {
            QueueRoleAsHost(player, roleType);
        }
        else
        {
            CommandErrorText($"Set {player.GetPlayerNameAndColor()} role as <{role.RoleColor}>{role.RoleName}</color> denied due to permission levels!");
            Rpc<RpcAddChatPrivate>.Instance.SendTo(player.Data.ClientId, new(CommandErrorText("Unable to set role due to permission levels!", true)));
        }
    }

    private static bool NeedsSpecialAttention(UserData data) =>
    (!data.HasAll() && data.IsSponsorTier3()) || !data.IsVerified();

    private static void QueueRoleAsHost(PlayerControl player, CustomRoleType roleType)
    {
        var data = player.Data;
        var role = Utils.GetCustomRoleClass(roleType);
        bool isAddon = role.IsAddon;
        bool isGhostRole = role.IsGhostRole;

        void SendResult(string text)
        {
            if (player.IsLocalPlayer())
            {
                CommandResultText(text);
                if (NeedsSpecialAttention(Main.MyData))
                {
                    foreach (var allUser in Main.AllPlayerControls.Where(pc => pc.ExtendedData().MyUserData.HasAll() && pc.ExtendedData().MyUserData.IsVerified(pc)))
                    {
                        Rpc<RpcAddChatPrivate>.Instance.SendTo(allUser.Data.ClientId, new(text));
                    }
                }
            }
            else
            {
                Rpc<RpcAddChatPrivate>.Instance.SendTo(player.Data.ClientId, new(text));
                var userData = player.ExtendedData().MyUserData;
                if (NeedsSpecialAttention(userData))
                {
                    if (Main.MyData.HasAll() && Main.MyData.IsVerified())
                    {
                        CommandResultText(text);
                    }
                }
            }
        }

        if (!isAddon)
        {
            if (!isGhostRole)
            {
                if (!CustomRoleManager.QueuedRoles.ContainsKey(data) || CustomRoleManager.QueuedRoles[data] != roleType)
                {
                    CustomRoleManager.QueuedRoles[data] = roleType;
                    SendResult($"Setting {player.GetPlayerNameAndColor()} Role to <{role.RoleColor}>{role.RoleName}</color> next game!");
                }
                else
                {
                    CustomRoleManager.QueuedRoles.Remove(data);
                    SendResult($"Unsetting {player.GetPlayerNameAndColor()} Role next game!");
                }
            }
            else
            {
                if (!CustomRoleManager.QueuedGhostRoles.ContainsKey(data) || CustomRoleManager.QueuedGhostRoles[data] != roleType)
                {
                    CustomRoleManager.QueuedGhostRoles[data] = roleType;
                    SendResult($"Setting {player.GetPlayerNameAndColor()} Role to <{role.RoleColor}>{role.RoleName}</color> next game upon death!");
                }
                else
                {
                    CustomRoleManager.QueuedGhostRoles.Remove(data);
                    SendResult($"Unsetting {player.GetPlayerNameAndColor()} Ghost Role next game!");
                }
            }
        }
        else
        {
            if (!CustomRoleManager.QueuedAddons.ContainsKey(data))
            {
                CustomRoleManager.QueuedAddons[data] = [];
            }

            if (!CustomRoleManager.QueuedAddons[data].Contains(roleType))
            {
                CustomRoleManager.QueuedAddons[data].Add(roleType);
                SendResult($"Adding Addon <{role.RoleColor}>{role.RoleName}</color> to {player.GetPlayerNameAndColor()} next game!");
            }
            else
            {
                CustomRoleManager.QueuedAddons[data].Remove(roleType);
                SendResult($"Removing Addon <{role.RoleColor}>{role.RoleName}</color> to {player.GetPlayerNameAndColor()} next game!");
            }
        }
    }
}

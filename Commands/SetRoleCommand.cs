using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
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

    public override bool ShowCommand() => GameState.IsHost && Main.MyData.IsSponsorTier3() || Main.MyData.HasAll();

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
                else if (Main.MyData.HasAll())
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

    public static void RequestQueueRole(PlayerControl player, CustomRoles roleType)
    {
        if (player.ExtendedData().MyUserData.HasAll() && GameState.IsHost)
        {
            QueueRoleAsHost(player, roleType);
        }
    }

    private static void QueueRoleAsHost(PlayerControl player, CustomRoles roleType)
    {
        var data = player.Data;
        var role = Utils.GetCustomRoleClass(roleType);
        bool isAddon = role.IsAddon;
        bool isGhostRole = role.IsGhostRole;

        void TryTellResult(string text)
        {
            if (Main.MyData.HasAll() || player.IsLocalPlayer())
            {
                CommandResultText(text);
            }
            if (!player.IsLocalPlayer())
            {
                Rpc<RpcAddChatPrivate>.Instance.SendTo(player.Data.ClientId, new(text));
            }
        }

        if (!isAddon) 
        {
            if (!isGhostRole)
            {
                if (!CustomRoleManager.QueuedRoles.ContainsKey(data) || CustomRoleManager.QueuedRoles[data] != roleType)
                {
                    CustomRoleManager.QueuedRoles[data] = roleType;
                    TryTellResult($"Setting {player.GetPlayerNameAndColor()} Role to <{role.RoleColor}>{role.RoleName}</color> next game!");
                }
                else
                {
                    CustomRoleManager.QueuedRoles.Remove(data);
                    TryTellResult($"Unsetting {player.GetPlayerNameAndColor()} Role next game!");
                }
            }
            else
            {
                if (!CustomRoleManager.QueuedGhostRoles.ContainsKey(data) || CustomRoleManager.QueuedGhostRoles[data] != roleType)
                {
                    CustomRoleManager.QueuedGhostRoles[data] = roleType;
                    TryTellResult($"Setting {player.GetPlayerNameAndColor()} Role to <{role.RoleColor}>{role.RoleName}</color> next game upon death!");
                }
                else
                {
                    CustomRoleManager.QueuedGhostRoles.Remove(data);
                    TryTellResult($"Unsetting {player.GetPlayerNameAndColor()} Ghost Role next game!");
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
                TryTellResult($"Adding Addon <{role.RoleColor}>{role.RoleName}</color> to {player.GetPlayerNameAndColor()} next game!");
            }
            else
            {
                CustomRoleManager.QueuedAddons[data].Remove(roleType);
                TryTellResult($"Removing Addon <{role.RoleColor}>{role.RoleName}</color> to {player.GetPlayerNameAndColor()} next game!");
            }
        }
    }
}

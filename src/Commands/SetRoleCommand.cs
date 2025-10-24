using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class SetRoleCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "SetRole";
    internal override uint ShortNamesAmount => 0;

    private PlayerArgument? PlayerArgument => (PlayerArgument)Arguments[0];
    private StringArgument? RoleArgument => (StringArgument)Arguments[1];
    internal SetRoleCommand()
    {
        Arguments = [new PlayerArgument(this, "Command.Arg.Player", ("{", "}")), new StringArgument(this, "Command.Arg.Role", ("{", "}"))];
        RoleArgument.GetArgSuggestions = () => { return CustomRoleManager.RolePrefabs.Select(role => role.RoleName.ToLower()).ToArray(); };
    }

    internal override bool ShowCommand() => (GameState.IsFreePlay || TBRGameSettings.Debugging.GetBool()) && Main.MyData.HasAll() && Main.MyData.IsVerified();

    internal override void Run()
    {
        var player = PlayerArgument.TryGetTarget();
        var role = CustomRoleManager.RolePrefabs
            .FirstOrDefault(role => role.RoleName.StartsWith(RoleArgument.Arg, StringComparison.OrdinalIgnoreCase));
        if (player != null)
        {
            if (role != null)
            {
                if (!role.IsAddon) CommandResultText($"Set {player.GetPlayerNameAndColor()} role to {Utils.GetCustomRoleNameAndColor(role.RoleType)}");
                else
                {
                    if (!player.Has(role.RoleType)) CommandResultText($"Added {Utils.GetCustomRoleNameAndColor(role.RoleType)} Add-on to {player.GetPlayerNameAndColor()}");
                    else CommandResultText($"Removed {Utils.GetCustomRoleNameAndColor(role.RoleType)} Add-on to {player.GetPlayerNameAndColor()}");
                }

                player.SendRpcSetCustomRole(role.RoleType, player.Has(role.RoleType));
            }
            else
            {
                CommandErrorText("Unable to find role");
            }
        }
    }
}

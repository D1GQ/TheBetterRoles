using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal class RoleCommand : BaseCommand
{
    internal virtual bool IsAddonCommand => false;
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => !IsAddonCommand ? "Role" : "Addon";
    internal override uint ShortNamesAmount => 0;

    private StringArgument? RoleArgument => (StringArgument)Arguments[0];
    internal RoleCommand()
    {
        Arguments = [new StringArgument(this, !IsAddonCommand ? "Command.Arg.Role" : "Command.Arg.Addon", ("{", "}"))];
        RoleArgument.GetArgSuggestions = () => { return CustomRoleManager.RolePrefabs.Where(role => role.IsAddon == IsAddonCommand).Select(role => role.RoleName.ToLower().Replace(' ', '_')).ToArray(); };
    }

    internal override void Run()
    {
        var role = !string.IsNullOrEmpty(RoleArgument.Arg) ? CustomRoleManager.RolePrefabs.Where(role => role.IsAddon == IsAddonCommand)
            .FirstOrDefault(role => role.RoleName.StartsWith(RoleArgument.Arg, StringComparison.OrdinalIgnoreCase)) : PlayerControl.LocalPlayer.Role();
        if (role != null && role.RoleType != RoleClassTypes.LobbyBehavior)
        {
            StringBuilder sb = new();
            sb.Append(Translator.GetString("Role", [Utils.GetCustomRoleNameAndColor(role.RoleType)]) + "\n");
            sb.Append(Translator.GetString("Role.Team", [$"<{Utils.GetCustomRoleTeamColorHex(role.RoleTeam)}>{Utils.GetCustomRoleTeamName(role.RoleTeam)}</color>"]) + "\n");
            sb.Append(Translator.GetString("Role.Category", [Utils.GetCustomRoleCategoryName(role.RoleCategory)]) + "\n\n");
            sb.Append($"{Utils.GetCustomRoleInfo(role.RoleType, true)}</size>");
            sb.AppendLine();
            sb.AppendLine();
            if (role.CanBeAssigned)
            {
                sb.Append($"<size=85%>{Translator.GetString(StringNames.RoleSettingsLabel)}:</size>\n");
                sb.Append(role?.RoleOptions.RoleOptionItem?.FormatOptionsToTextTree(85f) ?? string.Empty);
            }
            CommandResultText(sb.ToString());
        }
        else
        {
            CommandErrorText("Unable to find role");
        }
    }
}

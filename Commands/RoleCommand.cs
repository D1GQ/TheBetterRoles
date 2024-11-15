using System.Text;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class RoleCommand : BaseCommand
{
    public override string Name => "role";
    public override string Description => "Get information about a role";

    public RoleCommand()
    {
        _arguments = new Lazy<BaseArgument[]>(() => new BaseArgument[]
        {
            new StringArgument(this),
        });
        nameArgument.suggestion = "{Name}";
    }
    private readonly Lazy<BaseArgument[]> _arguments;
    public override BaseArgument[]? Arguments => _arguments.Value;

    private StringArgument? nameArgument => (StringArgument)Arguments[0];

    public virtual bool IsAddonCommand => false;

    public override void Run()
    {
        var role = CustomRoleManager.allRoles.Where(role => role.IsAddon == IsAddonCommand)
            .FirstOrDefault(role => role.RoleName.StartsWith(nameArgument.Arg, StringComparison.OrdinalIgnoreCase));
        if (role != null)
        {
            StringBuilder sb = new();
            sb.Append($"<size=125%>{string.Format(Translator.GetString("Role"), Utils.GetCustomRoleNameAndColor(role.RoleType))}\n");
            sb.Append($"{string.Format(Translator.GetString("Role.Team"), $"<{Utils.GetCustomRoleTeamColor(role.RoleTeam)}>{Utils.GetCustomRoleTeamName(role.RoleTeam)}</color>")}\n");
            sb.Append(string.Format(Translator.GetString("Role.Category"), $"{Utils.GetCustomRoleCategoryName(role.RoleCategory)}\n\n"));
            sb.Append($"{Utils.GetCustomRoleInfo(role.RoleType, true)}</size>");
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(role?.RoleOptionItem?.FormatOptionsToText(85f) ?? string.Empty);
            CommandResultText(sb.ToString());
        }
        else
        {
            CommandErrorText("Unable to find role");
        }
    }
}

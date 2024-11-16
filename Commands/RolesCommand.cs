using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Commands;

public class RolesCommand : BaseCommand
{
    public override string Name => "roles";
    public override string[] ShortNames => ["r"];
    public override string Description => "Get all enabled roles";
    public override void Run()
    {
        if (CustomRoleManager.allRoles.Where(role => role.GetChance() > 0).Any())
        {
            StringBuilder sb = new();

            string topBranch = "┏━━";
            string teamBranch = "┣━━";
            string categoryBranch = "┣━";
            string roleBranch = "┣▶ ";
            string vertical = "┃";

            var teamOrder = new[]
            {
                CustomRoleTeam.Impostor,
                CustomRoleTeam.Neutral,
                CustomRoleTeam.Apocalypse,
                CustomRoleTeam.Crewmate,
                CustomRoleTeam.None
            };

            var categoryOrder = Enum.GetValues(typeof(CustomRoleCategory)).Cast<CustomRoleCategory>().ToList();

            for (int i = 0; i < teamOrder.Length; i++)
            {
                var team = teamOrder[i];
                bool isAddonTeam = team == CustomRoleTeam.None;

                var rolesInTeam = CustomRoleManager.allRoles
                    .Where(role =>
                        role.GetChance() > 0 &&
                        (isAddonTeam ? role.IsAddon : role.RoleTeam == team && !role.IsAddon))
                    .GroupBy(role => role.RoleCategory)
                    .OrderBy(group => categoryOrder.IndexOf(group.Key));

                if (!rolesInTeam.Any()) continue;

                if (i == 0)
                {
                    sb.AppendLine($"{topBranch}<size=125%>▼<{Utils.GetCustomRoleTeamColor(team)}>{(isAddonTeam ? "Add-ons" : Utils.GetCustomRoleTeamName(team))}</color>▼</size>");
                }
                else
                {
                    sb.AppendLine($"{teamBranch}<size=125%>▼<{Utils.GetCustomRoleTeamColor(team)}>{(isAddonTeam ? "Add-ons" : Utils.GetCustomRoleTeamName(team))}</color>▼</size>");
                }

                var categories = rolesInTeam.ToList();
                for (int j = 0; j < categories.Count; j++)
                {
                    var categoryGroup = categories[j];
                    bool isLastCategory = j == categories.Count - 1;

                    sb.AppendLine($"{categoryBranch}▼<{Utils.GetCustomRoleTeamColor(team)}>{Utils.GetCustomRoleCategoryName(categoryGroup.Key)}</color>▼");

                    var roles = categoryGroup.ToList();
                    for (int k = 0; k < roles.Count; k++)
                    {
                        var role = roles[k];
                        bool isLastRole = k == roles.Count - 1 && isLastCategory;

                        var roleChance = role.RoleOptionItem?.FormatValueAsText() ?? "<color=#ff0000>Disabled</color>";

                        sb.AppendLine(isLastRole
                            ? $"┗▶ <b><size=85%><{role.RoleColor}>{role.RoleName}</color>: {roleChance}</size></b>"
                            : $"{roleBranch}<b><size=85%><{role.RoleColor}>{role.RoleName}</color>: {roleChance}</size></b>");
                    }
                }

                if (i < teamOrder.Length - 1 && teamOrder.Skip(i + 1).Any(t =>
                    CustomRoleManager.allRoles.Any(r => r.GetChance() > 0 && r.CanBeAssigned && (t == CustomRoleTeam.None ? r.IsAddon : r.RoleTeam == t && !r.IsAddon))))
                {
                    sb.AppendLine(vertical);
                }
            }

            CommandResultText(sb.ToString());
        }
        else
        {
            CommandErrorText("Unable to find enabled roles!");
        }
    }
}

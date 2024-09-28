using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheBetterRoles;

public class CustomGameManager
{
    public static void EndGame(List<NetworkedPlayerInfo>? Winners)
    {

    }

    public static void CheckWinConditions()
    {
        if (CheckImpostorWin())
        {

        }
        else if (CheckCrewmateWin())
        {

        }
        else if (CheckCustomWin() is PlayerControl player && player != null)
        {
        }
    }

    public static bool CheckImpostorWin()
    {
        var Impostors = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTeam.Impostor));
        var Players = Main.AllAlivePlayerControls.Where(pc => !pc.Is(CustomRoleTeam.Impostor));

        if (Impostors.Count() >= Players.Count())
        {
            return true;
        }

        return false;
    }

    public static bool CheckCrewmateWin() => Main.AllPlayerControls
        .Where(pc => pc.Is(CustomRoleTeam.Crewmate) && pc.BetterData().RoleInfo.RoleAssigned && pc.BetterData().RoleInfo.Role.HasTask)
        .All(pc => pc.myTasks.ToArray().All(t => t.IsComplete));

    public static PlayerControl? CheckCustomWin()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player.BetterData().RoleInfo.RoleAssigned && player.BetterData().RoleInfo.Role.WinCondition())
            {
                return player;
            }
        }

        return null;
    }
}

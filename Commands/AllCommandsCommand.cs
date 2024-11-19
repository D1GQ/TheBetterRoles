using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class AllCommandsCommand : BaseCommand
{
    public override string Name => "commands";
    public override string Description => "Get information about all commands";

    public override void Run()
    {
        BaseCommand?[] allNormalCommands = allCommands.Where(cmd => cmd.Type == CommandType.Normal).ToArray();
        BaseCommand?[] allSponsorCommands = allCommands.Where(cmd => cmd.Type == CommandType.Sponsor
                && (Main.MyData.IsSponsorTier1() && cmd.RequiredSponsorTier >= 1
                || Main.MyData.IsSponsorTier2() && cmd.RequiredSponsorTier >= 2
                || Main.MyData.IsSponsorTier3() && cmd.RequiredSponsorTier >= 3)).ToArray();
        BaseCommand?[] allDebugCommands = allCommands.Where(cmd => cmd.Type == CommandType.Debug).ToArray();
        string list;
        var open = "<color=#858585>┌──────── </color>";
        var mid = "<color=#858585>├ </color>";
        var close = "<color=#858585>└──────── </color>";
        list = "<color=#00751f><b><size=150%>Command List</size></b></color>\n" + open;

        if (allNormalCommands.Length > 0)
        {
            for (int i = 0; i < allNormalCommands.Length; i++)
            {
                if (i < allNormalCommands.Length)
                {
                    list += $"\n{mid}<color=#e0b700><b>{Main.CommandPrefix.Value}{allNormalCommands[i].Name}</b></color> <size=65%><color=#735e00>{allNormalCommands[i].Description}.</color></size>";
                }
            }
        }

        if (Main.MyData.IsSponsor() && allSponsorCommands.Length > 0)
        {
            list += "\n" + close + "\n";
            list += "<color=#00751f><b><size=150%>Sponsor Command List</size></b></color>\n" + open;
            for (int i = 0; i < allSponsorCommands.Length; i++)
            {
                if (i < allSponsorCommands.Length)
                {
                    list += $"\n{mid}<color=#e0b700><b>{Main.CommandPrefix.Value}{allSponsorCommands[i].Name}</b></color> <size=65%><color=#735e00>{allSponsorCommands[i].Description}.</color></size>";
                }
            }
        }

        if (Main.MyData.IsDev() && allDebugCommands.Length > 0)
        {
            list += "\n" + close + "\n";
            list += "<color=#00751f><b><size=150%>Debug Command List</size></b></color>\n" + open;
            for (int i = 0; i < allDebugCommands.Length; i++)
            {
                if (i < allDebugCommands.Length)
                {
                    list += $"\n{mid}<color=#e0b700><b>{Main.CommandPrefix.Value}{allDebugCommands[i].Name}</b></color> <size=65%><color=#735e00>{allDebugCommands[i].Description}.</color></size>";
                }
            }
        }

        list += "\n" + close;
        CommandResultText(list);
    }
}

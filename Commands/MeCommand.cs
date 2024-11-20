using System.Text;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Commands;

public class MeCommand : BaseCommand
{
    public override string Name => "me";
    public override string Description => "Get information about your user data";

    public override void Run()
    {
        StringBuilder sb = new();
        var format1 = "┌ •";
        var format2 = "├ •";
        var format3 = "└ •";
        sb.Append($"<size=150%><b>{PlayerControl.LocalPlayer.GetPlayerNameAndColor()} UserData:</b></size>\n");
        sb.Append($"{format1} <color=#008D00>Sponsor<#8A8A8A>:</color> {FormatBool(Main.MyData.IsSponsor())}</color>\n");
        if (Main.MyData.IsSponsor()) sb.Append($"{format2} <color=#008D00>Sponsor Tier: {GetSponsorTier()}</color>\n");
        sb.Append($"{format2} <color=#8D1FB1>Tester<#8A8A8A>:</color> {FormatBool(Main.MyData.IsTester())}</color>\n");
        sb.Append($"{format3} <color=#2F40FF>Dev<#8A8A8A>:</color> {FormatBool(Main.MyData.IsDev())}</color>\n");
        CommandResultText(sb.ToString());
    }

    private static string FormatBool(bool @bool) => @bool ? "<#00FF00>✓</color>" : "<#FF0600>〤</color>";

    private static string GetSponsorTier()
    {
        if (Main.MyData.IsSponsorTier3())
        {
            return "<#00FF83>3</color>";
        }
        else if (Main.MyData.IsSponsorTier2())
        {
            return "<#00FBFF>2</color>";
        }
        else if (Main.MyData.IsSponsorTier1())
        {
            return "<#007CFF>1</color>";
        }
        else 
        { 
            return "<#818181>0</color>"; 
        }
    }
}

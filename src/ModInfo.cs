using TheBetterRoles.Items.Enums;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles;

internal class ModInfo
{
    internal static readonly ReleaseTypes ReleaseBuildType = ReleaseTypes.Alpha;
    internal const string BetaNum = "0";
    internal const string AlphaNum = "10";
    internal const string HotfixNum = "0";
    internal const bool IsHotFix = false;
    internal const string PluginGuid = "com.ten.thebetterroles";
    internal const string PluginVersion = "0.0.1";
    internal const string ReleaseDate = "10.21.2025"; // mm/dd/yyyy
    internal const string Github = "https://github.com/D1GQ/TheBetterRoles-Public";
    internal const string Discord = "https://discord.gg/vjYrXpzNAn";
    internal static bool IsGuestBuild { get; private set; } = false && !(Main.MyData.IsSponsor() || Main.MyData.IsTester() || Main.MyData.IsDev());
}
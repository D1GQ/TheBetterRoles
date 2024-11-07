using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Innersloth.IO;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using System.Security.Cryptography;
using System.Text;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles;

public enum ReleaseTypes : int
{
    Release,
    Beta,
    Dev,
}

[BepInPlugin(PluginGuid, "TheBetterRoles", PluginVersion)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients | ModFlags.DisableServerAuthority)]
public class Main : BasePlugin
{
    public static readonly ReleaseTypes ReleaseBuildType = ReleaseTypes.Beta;
    public const string BetaNum = "1";
    public const string HotfixNum = "0";
    public const bool IsHotFix = false;
    public const string PluginGuid = "com.ten.thebetterroles";
    public const string PluginVersion = "0.0.1";
    public const string ReleaseDate = "10.12.2024"; // mm/dd/yyyy
    public const string Github = "https://github.com/EnhancedNetwork/BetterAmongUs-Public";
    public const string Discord = "https://discord.gg/ten";
    public static bool IsGuestBuild { get; private set; } = false;

    public static string modSignature
    {
        get
        {
            string GetHash(string hash)
            {
                using SHA256 sha256 = SHA256.Create();
                byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hash));
                string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();
                return sha256Hash[..8];
            }

            var versionData = new StringBuilder()
                .Append(Enum.GetName(typeof(ReleaseTypes), ReleaseBuildType))
                .Append(BetaNum)
                .Append(HotfixNum)
                .Append(PluginGuid)
                .Append(GetVersionText().Replace(" ", "."))
                .Append(ReleaseDate)
                .Append(Github)
                .Append(Discord)
                .Append(string.Join(".", CustomRoleManager.allRoles.Select(role => role.GetType().Name)))
                .Append(string.Join(".", CustomRoleManager.allRoles.Select(role => role.RoleId)))
                .Append(string.Join(".", CustomRoleManager.allRoles.Select(role => role.RoleUID)))
                .Append(string.Join(".", Enum.GetNames(typeof(TargetType))))
                .Append(string.Join(".", Enum.GetNames(typeof(ReactorRPCs))))
                .Append(string.Join(".", Enum.GetNames(typeof(CustomRPC))))
                .Append(string.Join(".", Enum.GetNames(typeof(CustomRoles))))
                .ToString();

            return GetHash(versionData);
        }
    }

    public static string GetVersionText(bool newLine = false)
    {
        string text = string.Empty;

        string newLineText = newLine ? "\n" : " ";

        switch (ReleaseBuildType)
        {
            case ReleaseTypes.Release:
                text = $"v{TheBetterRolesVersion}";
                break;
            case ReleaseTypes.Beta:
                text = $"v{TheBetterRolesVersion}{newLineText}Beta {Main.BetaNum}";
                break;
            case ReleaseTypes.Dev:
                text = $"v{TheBetterRolesVersion}{newLineText}Dev {Main.ReleaseDate}";
                break;
            default:
                break;
        }


        if (IsHotFix)
            text += $" Hotfix {HotfixNum}";

#if DEBUG_MULTIACCOUNTS
        text += $"{newLineText}<color=#dc00ff>MultiAccounts</color>";
#endif

        return text;
    }

    public static Harmony Harmony { get; } = new Harmony(PluginGuid);

    public static string TheBetterRolesVersion => PluginVersion;
    public static string AmongUsVersion => Application.version;

    public static PlatformSpecificData PlatformData => Constants.GetPlatformData();

    public static List<string> SupportedAmongUsVersions =
    [
        "2024.10.29",
        "2024.9.4",
    ];

    public static string[] DevUser =
    [
        "8f23c48e2+3e249bee5",
    ];

    public static IGameOptions? CurrentOptions => GameOptionsManager.Instance?.CurrentGameOptions;
    public static NormalGameOptionsV08? NormalOptions => GameOptionsManager.Instance?.currentNormalGameOptions;
    public static HideNSeekGameOptionsV08? HideNSeekOptions => GameOptionsManager.Instance?.currentHideNSeekGameOptions;
    public static PlayerControl[] AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(pc => pc != null).ToArray();

    public static PlayerControl[] AllAlivePlayerControls => AllPlayerControls.ToArray().Where(pc => !pc.Data.IsDead).ToArray();
    public static DeadBody[] AllDeadBodys => UnityEngine.Object.FindObjectsOfType<DeadBody>().ToArray();
    public static Vent[] AllVents => UnityEngine.Object.FindObjectsOfType<Vent>();
    public static Vent[] AllEnabledVents => UnityEngine.Object.FindObjectsOfType<Vent>().Where(vent => vent.IsEnabled()).ToArray();

    public static ManualLogSource? Logger;

    public override void Load()
    {
        try
        {
            Preset = Config.Bind("Better Settings", "Preset", 1);
            ConsoleManager.CreateConsole();
            ConsoleManager.SetConsoleTitle("Among Us - TBR Console");
            ConsoleManager.ConfigPreventClose.Value = true;
            Logger = BepInEx.Logging.Logger.CreateLogSource(PluginGuid);

            // Add custom components
            {
                AddComponent<AssetBundleManager>();
                AddComponent<ExtendedPlayerInfo>();
                AddComponent<GuessManager>();
            }

            CheckRoleIds();

            BetterDataManager.SetUp();
            BetterDataManager.LoadData();
            LoadOptions();
            Translator.Init();
            Harmony.PatchAll();
            GameSettingMenuPatch.SetupSettings(true);
            CustomColors.Load();
            CustomSoundsManager.Load();
            SubmergedCompatibility.Initialize();

            if (PlatformData.Platform == Platforms.StandaloneSteamPC)
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "steam_appid.txt"), "945360");

            if (File.Exists(Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt")))
                File.WriteAllText(Path.Combine(BetterDataManager.filePathFolder, "betterrole-previous-log.txt"), File.ReadAllText(Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt")));

            File.WriteAllText(Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt"), "");
            TheBetterRoles.Logger.Log("The Better Roles successfully loaded!");

            string SupportedVersions = string.Empty;
            foreach (string text in SupportedAmongUsVersions.ToArray())
                SupportedVersions += $"{text} ";
            TheBetterRoles.Logger.Log($"TheBetterRoles {TheBetterRolesVersion}-{ReleaseDate} - [{AmongUsVersion} --> {SupportedVersions.Substring(0, SupportedVersions.Length - 1)}] {Utils.GetPlatformName(PlatformData.Platform)}");
        }
        catch (Exception ex)
        {
            TheBetterRoles.Logger.Error(ex);
        }
    }

    private void CheckRoleIds()
    {
        Dictionary<int, List<string>> idToRolesMap = [];

        foreach (var role in CustomRoleManager.allRoles)
        {
            var id = role.RoleId;

            if (idToRolesMap.ContainsKey(id))
            {
                idToRolesMap[id].Add(role.GetType().Name);
            }
            else
            {
                idToRolesMap[id] = [role.GetType().Name];
            }
        }

        foreach (var kvp in idToRolesMap)
        {
            if (kvp.Value.Count > 1)
            {
                var rolesWithSameId = string.Join(", ", kvp.Value);
                TheBetterRoles.Logger.Warning($"Duplicate RoleId detected: Id ({kvp.Key}) is assigned to roles: {rolesWithSameId}. " +
                    "This will cause weird side effects and needs to be changed as soon as possible!");
            }
        }
    }

    public static ConfigEntry<int> Preset { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ChatDarkMode { get; private set; }
    public static ConfigEntry<bool> DisableLobbyTheme { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<bool> ShowFPS { get; private set; }
    public static ConfigEntry<string> CommandPrefix { get; set; }
    private void LoadOptions()
    {
        ForceOwnLanguage = Config.Bind("Better Options", "ForceOwnLanguage", false);
        ChatDarkMode = Config.Bind("Better Options", "ChatDarkMode", true);
        DisableLobbyTheme = Config.Bind("Better Options", "DisableLobbyTheme", true);
        UnlockFPS = Config.Bind("Better Options", "UnlockFPS", false);
        ShowFPS = Config.Bind("Better Options", "ShowFPS", false);
        CommandPrefix = Config.Bind("Client Options", "CommandPrefix", "/");
    }

    public static string GetDataPathToAmongUs() => FileIO.GetRootDataPath();
    public static string GetGamePathToAmongUs() => Environment.CurrentDirectory;
}

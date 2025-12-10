using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Patches.UI.GameSettings;
using UnityEngine;
using static TheBetterRoles.Patches.Client.AddressableAssetPatch;

namespace TheBetterRoles;

[BepInPlugin(ModInfo.PluginGuid, "TheBetterRoles", ModInfo.PluginVersion)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id, BepInDependency.DependencyFlags.HardDependency)]
#if DEBUG_MULTIACCOUNTS
[ReactorModFlags(ModFlags.DisableServerAuthority)]
#else
[ReactorModFlags(ModFlags.RequireOnAllClients | ModFlags.DisableServerAuthority)]
#endif
internal class Main : BasePlugin
{
    internal static BasePlugin? Plugin { get; private set; }
    internal static UserData? MyData = ReadOnlyManager.AllUsers[0];

    internal static uint ModSignature => modSignature.Value;
    private static readonly Lazy<uint> modSignature = new(() =>
    {
        string dllPath = Assembly.GetExecutingAssembly().Location;
        if (!File.Exists(dllPath))
            return 0;

        using FileStream stream = File.OpenRead(dllPath);
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(stream);
        string hashSubstring = BitConverter.ToString(hashBytes).Replace("-", "").ToLower()[..8];
        return Convert.ToUInt32(hashSubstring, 16);
    });

    internal static int GetGameInstanceIndex()
    {
        Process currentProcess = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
        processes = processes.OrderBy(p => p.StartTime).ToArray();
        for (int i = 0; i < processes.Length; i++)
        {
            if (processes[i].Id == currentProcess.Id)
            {
                return i;
            }
        }
        return -1;
    }

    internal static string GetVersionText(bool newLine = false)
    {
        string text = string.Empty;

        string newLineText = newLine ? "\n" : " ";

        switch (ModInfo.ReleaseBuildType)
        {
            case ReleaseTypes.Release:
                text = $"v{TheBetterRolesVersion}";
                break;
            case ReleaseTypes.Beta:
                text = $"v{TheBetterRolesVersion}{newLineText}Beta {ModInfo.BetaNum}";
                break;
            case ReleaseTypes.Alpha:
                text = $"v{TheBetterRolesVersion}{newLineText}Alpha {ModInfo.AlphaNum}";
                break;
            default:
                break;
        }


        if (ModInfo.IsHotFix)
#pragma warning disable CS0162 // Unreachable code detected
            text += $" Hotfix {ModInfo.HotfixNum}";
#pragma warning restore CS0162 // Unreachable code detected

        return text;
    }

    internal static Harmony Harmony { get; } = new Harmony(ModInfo.PluginGuid);

    internal static string TheBetterRolesVersion => ModInfo.PluginVersion;
    internal static string AppVersion => Application.version;
    internal static string AmongUsVersion => ReferenceDataManager.Instance.Refdata.userFacingVersion;

    internal static PlatformSpecificData PlatformData => Constants.GetPlatformData();

    internal static List<string> SupportedAmongUsVersions =
    [
        "2025.11.18",
    ];

    internal static IGameOptions? CurrentOptions => GameOptionsManager.Instance?.CurrentGameOptions;
    internal static NormalGameOptionsV10? NormalOptions => GameOptionsManager.Instance?.currentNormalGameOptions;
    internal static HideNSeekGameOptionsV10? HideNSeekOptions => GameOptionsManager.Instance?.currentHideNSeekGameOptions;
    internal static List<PlayerControl> AllPlayerControls = [];
    internal static List<PlayerControl> AllRealPlayerControls => AllPlayerControls.Where(pc => !pc.ExtendedPC().IsFake).ToList();
    internal static List<PlayerControl> AllAlivePlayerControls => AllPlayerControls.Where(pc => pc.IsAlive()).ToList();
    internal static DeadBody[] AllDeadBodys => UnityEngine.Object.FindObjectsOfType<DeadBody>().ToArray();
    internal static Vent[] AllVents => ShipStatus.Instance != null ? ShipStatus.Instance.AllVents : [];
    internal static Vent[] AllEnabledVents => AllVents.Where(vent => vent.IsEnabled()).ToArray();
    internal static Console[] AllConsoles => UnityEngine.Object.FindObjectsOfType<Console>();

    internal static ManualLogSource? Logger { get; private set; }

    public override void Load()
    {
        try
        {
            Plugin = this;

            Preset = Config.Bind("Better Settings", "Preset", 1);

            ConsoleManager.CreateConsole();
            ConsoleManager.SetConsoleTitle("Among Us - TBR Console");
            ConsoleManager.ConfigPreventClose.Value = true;
            if (ConsoleManager.ConfigConsoleEnabled.Value) ConsoleManager.DetachConsole();
            ConsoleManager.ConfigConsoleEnabled.Value = false;
            Logger = BepInEx.Logging.Logger.CreateLogSource(ModInfo.PluginGuid);
            var customLogListener = new CustomLogListener();
            BepInEx.Logging.Logger.Listeners.Add(customLogListener);
            ConsoleManager.SetConsoleColor(ConsoleColor.Cyan);
            ConsoleManager.ConsoleStream.WriteLine($"  _______ _          ____       _   _            _____       _           \r\n |__   __| |        |  _ \\     | | | |          |  __ \\     | |          \r\n    | |  | |__   ___| |_) | ___| |_| |_ ___ _ __| |__) |___ | | ___  ___ \r\n    | |  | '_ \\ / _ \\  _ < / _ \\ __| __/ _ \\ '__|  _  // _ \\| |/ _ \\/ __|\r\n    | |  | | | |  __/ |_) |  __/ |_| ||  __/ |  | | \\ \\ (_) | |  __/\\__ \\\r\n    |_|  |_| |_|\\___|____/ \\___|\\__|\\__\\___|_|  |_|  \\_\\___/|_|\\___||___/\r\n<=========================================================================>");

            Prefab.CopyPrefab<Mushroom>();

            // Add custom components
            {
                RegisterAllMonoBehavioursInAssembly();
                AddComponent<ModUpdateManager>();
                AddComponent<CoroutineManager>();
            }

            InstanceAttribute.RegisterAll();
            CheckRoleIds();

            LoadOptions();
            GithubAPI.Connect();
            TBRDataManager.SetUp();
            TBRDataManager.LoadData();
            Translator.Init();
            FileChecker.Initialize();
            Harmony.PatchAll();
            LoadAssetPatch.Patch();
            GameSettingMenuPatch.SetupSettings(true);
            CustomColors.ReplaceColorPalette();
            AssetBundles.LoadAssetBundles();
            CustomSystemTypes.Initialize();

            if (PlatformData.Platform == Platforms.StandaloneSteamPC)
                File.WriteAllText(Path.Combine(GetGamePathToAmongUs(), "steam_appid.txt"), "945360");

            if (File.Exists(TBRDataManager.LogFile))
                File.WriteAllText(TBRDataManager.PreviousLogFile, TBRDataManager.LogFile);

            File.WriteAllText(TBRDataManager.LogFile, "");
            File.WriteAllText(TBRDataManager.NetworkLogFile, "");

            TheBetterRoles.Logger.Log("Main successfully loaded!");
            string SupportedVersions = string.Join(" > ", SupportedAmongUsVersions);
            TheBetterRoles.Logger.Log($"TheBetterRoles {GetVersionText().Replace(' ', '.')}/{ModInfo.ReleaseDate} - [{AppVersion} --> {SupportedVersions}] {Utils.GetPlatformName(PlatformData.Platform)} - Dll({ModSignature})");
        }
        catch (Exception ex)
        {
            TheBetterRoles.Logger.Error(ex);
        }
    }

    private static bool hasLateLoad;
    internal static void LateLoad()
    {
        if (hasLateLoad) return;
        hasLateLoad = true;
        CustomSoundsManager.CreateInstance();
        PrefabManager.CatchPrefabs();
    }

    internal static void RegisterAllMonoBehavioursInAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var monoBehaviourTypes = assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
            .OrderBy(type => type.Name);

        foreach (var type in monoBehaviourTypes)
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
            }
            catch (Exception ex)
            {
                TheBetterRoles.Logger.Error($"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
            }
        }
    }

    private void CheckRoleIds()
    {
        Dictionary<int, List<string>> idToRolesMap = [];

        foreach (var role in CustomRoleManager.RolePrefabs)
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

    internal static ConfigEntry<bool> ConnectGithubAPI { get; private set; }
    internal static ConfigEntry<int> Preset { get; private set; }
    internal static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    internal static ConfigEntry<bool> ChatDarkMode { get; private set; }
    internal static ConfigEntry<bool> DisableLobbyTheme { get; private set; }
    internal static ConfigEntry<bool> UnlockFPS { get; private set; }
    internal static ConfigEntry<bool> ShowFPS { get; private set; }
    internal static ConfigEntry<string> CommandPrefix { get; set; }
    private void LoadOptions()
    {
        ConnectGithubAPI = Config.Bind("API", "ConnectGithubAPI", true, "Connect to Github API on launch.");
        ForceOwnLanguage = Config.Bind("Better Options", "ForceOwnLanguage", false);
        ChatDarkMode = Config.Bind("Better Options", "ChatDarkMode", true);
        DisableLobbyTheme = Config.Bind("Better Options", "DisableLobbyTheme", true);
        UnlockFPS = Config.Bind("Better Options", "UnlockFPS", false);
        ShowFPS = Config.Bind("Better Options", "ShowFPS", false);
        CommandPrefix = Config.Bind("Client Options", "CommandPrefix", "/");
    }

    internal static string GetDataPathToAmongUs() => Application.persistentDataPath;
    internal static string GetGamePathToAmongUs() => Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
}
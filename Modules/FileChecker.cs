using System.Collections.ObjectModel;
using UnityEngine;

namespace TheBetterRoles.Modules;

class FileChecker
{
    private static bool Enabled = false;
    private static readonly ReadOnlyCollection<string> UnsupportedBepInExMods = new(new List<string> { "TOHE", "YuAntiCheat" }); // Put BepInEx BepInPlugin name, not dll name here lol.
    private static readonly ReadOnlyCollection<string> BannedBepInExMods = new(new List<string> { "MalumMenu", "MalumMenu-Yu", "MalumMenuYu"/*, "AUnlocker" */}); // Put BepInEx BepInPlugin name, not dll name here lol.
    private static readonly ReadOnlyCollection<string> KeyWordsInVersionInfo = new(new List<string> { "Malum", "Sicko", "AUM" }); // Banned words for version text
    public static string UnauthorizedReason = string.Empty;
    public static List<string> CheatTags = []; // For API report
    public static bool HasTrySpoofFriendCode = false;
    public static bool HasUnauthorizedFile = false;
    public static bool HasShownPopUp = false;
    private static float waitTime = 5f;

    // Set up if unauthorized files have been found.
    public static void UpdateUnauthorizedFiles()
    {
#if DEBUG || DEBUG_MULTIACCOUNTS
        if (GameState.IsDev)
        {
            HasTrySpoofFriendCode = false;
        }
#endif

        if (Enabled == false) return;

        GameObject PlayButton = GameObject.Find("Main Buttons/PlayButton");

        // Disable play button
        if (PlayButton != null)
        {
            if (EOSManager.Instance.userId == null || HasTrySpoofFriendCode)
            {
                PlayButton.GetComponent<BoxCollider2D>().enabled = false;
                GameObject.Find("Main Buttons/PlayButton/Inactive").GetComponent<SpriteRenderer>().color = Color.gray;
            }
            else
            {
                PlayButton.GetComponent<BoxCollider2D>().enabled = true;
                GameObject.Find("Main Buttons/PlayButton/Inactive").GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f);
            }
        }

        // Unauthorized file or ban detected.
        if (HasUnauthorizedFile)
        {
            if (GameState.IsInGame)
            {
                Utils.DisconnectSelf(OnlineMsg);
            }

            GameObject playOnlineButton = GameObject.Find("PlayOnlineButton");

            if (playOnlineButton != null)
            {
                PassiveButton PassiveButtonComponent = playOnlineButton.GetComponent<PassiveButton>();
                PlayOnlineButtonSprite PlayOnlineButtonSpriteComponent = playOnlineButton.GetComponent<PlayOnlineButtonSprite>();

                if (PassiveButtonComponent != null)
                {
                    if (PassiveButtonComponent != null)
                        PassiveButtonComponent.enabled = false;

                    PlayOnlineButtonSpriteComponent?.SetGreyscale();
                }
            }

            Utils.DisconnectAccountFromOnline();

            SoundManager.instance?.ChangeMusicVolume(0);
            return;
        }

        if (EOSManager.Instance.editAccountUsername.gameObject.active || EOSManager.Instance.askToMergeAccount.gameObject.active)
        {
            HasTrySpoofFriendCode = true;
        }

        if (GameState.IsInGame && GameState.IsLobby)
        {
            waitTime -= Time.deltaTime;

            if (waitTime <= 0)
            {
                CheckIfUnauthorizedFiles();
                waitTime = 5f;
            }
        }
        else
        {
            waitTime = 5f;
        }
    }

    private static string UnauthorizedTextDetectedMsg => Translator.GetString("FileChecker.UnauthorizedTextDetectedMsg");
    private static string UnauthorizedFileMsg => Translator.GetString("FileChecker.UnauthorizedFileMsg");
    private static string OnlineMsg => Translator.GetString("FileChecker.OnlineMsg");
    private static string UnsupportedBepInExModMsg => Translator.GetString("FileChecker.UnsupportedBepInExModMsg");
    private static string BannedBepInExModMsg => Translator.GetString("FileChecker.BannedBepInExModMsg");

    // Check if there's any unauthorized files.
    public static bool CheckIfUnauthorizedFiles()
    {
        if (Enabled == false) return false;

        // Get user info for later use with API.
        string ClientUserName = string.Empty;
        string ClientFriendCode = string.Empty;
        string ClientPUIDHash = string.Empty;

        if (!GameState.IsInGame)
        {
            ClientUserName = GameObject.Find("AccountTab")?.GetComponent<AccountTab>()?.userName.text;
            ClientFriendCode = EOSManager.Instance.friendCode;
            ClientPUIDHash = Utils.GetHashPuid(EOSManager.Instance.ProductUserId);
        }
        else
        {
            if (PlayerControl.LocalPlayer?.Data != null)
            {
                ClientUserName = PlayerControl.LocalPlayer.Data.PlayerName;
                ClientFriendCode = PlayerControl.LocalPlayer.Data.FriendCode;
                ClientPUIDHash = Utils.GetHashPuid(PlayerControl.LocalPlayer);
            }
        }

#if DEBUG || DEBUG_MULTIACCOUNTS
        if (GameState.IsDev)
        {
            Enabled = false;
            return false;
        }
#endif

        // Check for Banned BepInEx Mods
        foreach (var bannedMod in BannedBepInExMods)
        {
            if (IsBepInExModLoaded(bannedMod))
            {
                if (!HasUnauthorizedFile) UnauthorizedReason = BannedBepInExModMsg;
                if (!CheatTags.Contains($"{bannedMod}-BepInEx")) CheatTags.Add($"{bannedMod}-BepInEx");
                HasUnauthorizedFile = true;
            }
        }

        // Check for Unsupported BepInEx Mods
        foreach (var unsupportedMod in UnsupportedBepInExMods)
        {
            if (IsBepInExModLoaded(unsupportedMod))
            {
                if (!HasUnauthorizedFile) UnauthorizedReason = UnsupportedBepInExModMsg;
                HasUnauthorizedFile = true;
            }
        }

        // Check for version.dll
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "version.dll")))
        {
            string versiondll = "<color=#ffffff>'</color><color=#ffca2b>version.dll</color><color=#ffffff>'</color>";
            if (!HasUnauthorizedFile) UnauthorizedReason = string.Format(UnauthorizedTextDetectedMsg, versiondll);
            if (!CheatTags.Contains("version.dll")) CheatTags.Add("version.dll");
            HasUnauthorizedFile = true;
        }

        // Check for banned words in VersionInfo display. Aka check cheat developers ego
        foreach (var WordInVersionInfo in KeyWordsInVersionInfo)
        {
            if (!GameState.IsInGame)
            {
                if (UnityEngine.Object.FindFirstObjectByType<VersionShower>().text.text.ToLower().Contains(WordInVersionInfo.ToLower()))
                {
                    if (!HasUnauthorizedFile) UnauthorizedReason = UnauthorizedFileMsg;
                    if (!CheatTags.Contains($"{WordInVersionInfo}-VersionInfo")) CheatTags.Add($"{WordInVersionInfo}-VersionInfo");
                    HasUnauthorizedFile = true;
                }
            }
            else
            {
                if (UnityEngine.Object.FindFirstObjectByType<PingTracker>().text.text.ToLower().Contains(WordInVersionInfo.ToLower()))
                {
                    if (!HasUnauthorizedFile) UnauthorizedReason = UnauthorizedFileMsg;
                    if (!CheatTags.Contains($"{WordInVersionInfo}-VersionInfo")) CheatTags.Add($"{WordInVersionInfo}-VersionInfo");
                    HasUnauthorizedFile = true;
                }
            }
        }

        // Check for Sicko leftover files
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-settings.json")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-prev-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-config")))
        {
            if (!HasUnauthorizedFile) UnauthorizedReason = UnauthorizedFileMsg;
            if (!CheatTags.Contains("Sicko-Menu-Files")) CheatTags.Add("Sicko-Menu-Files");
            HasUnauthorizedFile = true;
        }

        // Check for AUM leftover files
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "settings.json")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-prev-log.txt")))
        {
            if (!HasUnauthorizedFile) UnauthorizedReason = UnauthorizedFileMsg;
            if (!CheatTags.Contains("AUM-Menu-Files")) CheatTags.Add("AUM-Menu-Files");
            HasUnauthorizedFile = true;
        }

        // ----------- Unused Until API support is added! -----------

        // Combine Player information and Tags
        string tagsAsString = string.Join(" - ", CheatTags);
        string playerInfo = $"{ClientUserName}.{ClientFriendCode}.{ClientPUIDHash} - {tagsAsString}"; // If Detection goes off send this information to the API database!

        // ----------------------------------------------------------

        return HasUnauthorizedFile;
    }

    // Get all loaded BepInEx mods and check if one is on the ban list.
    private static bool IsBepInExModLoaded(string modName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName.Contains(modName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.GameSettings;

class TBRTabs
{
    internal static OptionTab? GameSettings;
    internal static OptionTab? SystemSettings;
    internal static OptionTab? CrewmateRoles;
    internal static OptionTab? ImpostorRoles;
    internal static OptionTab? NeutralRoles;
    internal static OptionTab? ApocalypseRoles;
    internal static OptionTab? Addons;
}

[HarmonyPatch(typeof(GameSettingMenu))]
internal class GameSettingMenuPatch
{
    internal static List<OptionTab> Tabs = [];
    internal static int ActiveTab = 0;
    internal static bool Preload = false;

    internal static void SetupSettings(bool IsPreload = false, bool isSync = false)
    {
        if (GameSettingMenu.Instance != null && IsPreload && isSync)
        {
            GameSettingMenu.Instance.Close();
        }

        Preload = IsPreload;
        OptionItem.AllTBROptionsTemp.Clear();

        SetUpGameSettings(IsPreload, isSync);
        SetUpModSettings(IsPreload, isSync);
        SetUpRoleSettings(IsPreload, isSync);

        Preload = false;
    }

    internal static void SetUpGameSettings(bool IsPreload, bool isSync)
    {
        bool mapFlag = GameState.IsLobby && GameState.IsHost && !GameState.IsFreePlay;

        TBRTabs.GameSettings = OptionTab.Create(1, "Setting.Tab.GameSettings", "Setting.Description.GameSettings", Color.green * 0.8f, mapFlag);
        TBRTabs.SystemSettings = OptionTab.Create(2, "Setting.Tab.SystemSettings", "Setting.Description.SystemSettings", Color.yellow);
        TBRTabs.CrewmateRoles = OptionTab.Create(3, "Setting.Tab.CrewmateRoles", "Setting.Description.CrewmateRoles", Color.cyan);
        TBRTabs.ImpostorRoles = OptionTab.Create(4, "Setting.Tab.ImpostorRoles", "Setting.Description.ImpostorRoles", Color.red);
        TBRTabs.NeutralRoles = OptionTab.Create(5, "Setting.Tab.NeutralRoles", "Setting.Description.NeutralRoles", Color.gray);
        TBRTabs.ApocalypseRoles = OptionTab.Create(6, "Setting.Tab.ApocalypseRoles", "Setting.Description.ApocalypseRoles", Color.gray - new Color(0.25f, 0.25f, 0.25f, 0f));
        TBRTabs.Addons = OptionTab.Create(7, "Setting.Tab.Addons", "Setting.Description.Addons", Color.magenta);
        OptionTab.AlignButtons();

        // Next setting Id to use: 42

        OptionHeaderItem.Create(TBRTabs.GameSettings, "Setting.Title.MapSettings", !mapFlag ? 0.1f : 1.5f);

        TBRGameSettings.ReverseSkeld = OptionCheckboxItem.Create(34, TBRTabs.GameSettings, "Setting.ReverseSkeld", false);
        TBRGameSettings.ReverseMira = OptionCheckboxItem.Create(35, TBRTabs.GameSettings, "Setting.ReverseMira", false);
        TBRGameSettings.ReversePolus = OptionCheckboxItem.Create(36, TBRTabs.GameSettings, "Setting.ReversePolus", false);
        TBRGameSettings.ReverseAirship = OptionCheckboxItem.Create(37, TBRTabs.GameSettings, "Setting.ReverseAirship", false);
        TBRGameSettings.ReverseFungle = OptionCheckboxItem.Create(38, TBRTabs.GameSettings, "Setting.ReverseFungle", false);

        OptionDividerItem.Create(TBRTabs.GameSettings);

        TBRGameSettings.BetterPolus = OptionCheckboxItem.Create(33, TBRTabs.GameSettings, "Setting.BetterPolus", false);

        OptionHeaderItem.Create(TBRTabs.GameSettings, "Setting.Title.PlayerSettings");

        OptionTitleItem.Create(TBRTabs.GameSettings, Translator.GetString("Setting.Title.PlayerSettings.Impostor").ToColor(Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Impostor)));
        VanillaGameSettings.ImpostorVision = OptionFloatItem.Create(0, TBRTabs.GameSettings, "Setting.ImpostorVision", (0.25f, 5f, 0.25f), 1.25f, ("", "x"), vanillaOption: FloatOptionNames.ImpostorLightMod);
        VanillaGameSettings.KillCooldown = OptionFloatItem.Create(1, TBRTabs.GameSettings, "Setting.KillCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), vanillaOption: FloatOptionNames.KillCooldown);
        VanillaGameSettings.KillDistance = OptionStringItem.Create(2, TBRTabs.GameSettings, "Setting.KillDistance",
                ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 1, vanillaOption: Int32OptionNames.KillDistance);

        OptionDividerItem.Create(TBRTabs.GameSettings);
        OptionTitleItem.Create(TBRTabs.GameSettings, Translator.GetString("Setting.Title.PlayerSettings.Crewmate").ToColor(Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate)));
        VanillaGameSettings.PlayerVision = OptionFloatItem.Create(3, TBRTabs.GameSettings, "Setting.PlayerVision", (0.25f, 5f, 0.25f), 0.75f, ("", "x"), vanillaOption: FloatOptionNames.CrewLightMod);
        VanillaGameSettings.PlayerSpeed = OptionFloatItem.Create(4, TBRTabs.GameSettings, "Setting.PlayerSpeed", (0.25f, 5f, 0.25f), 1.25f, ("", "x"), vanillaOption: FloatOptionNames.PlayerSpeedMod);
        TBRGameSettings.ReportDistance = OptionStringItem.Create(39, TBRTabs.GameSettings, "Setting.ReportDistance",
        ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 2);

        OptionHeaderItem.Create(TBRTabs.GameSettings, "Setting.Title.MeetingSettings");

        VanillaGameSettings.EmergencyMeetings = OptionIntItem.Create(5, TBRTabs.GameSettings, "Setting.EmergencyMeetings", (0, 100, 1), 1, vanillaOption: Int32OptionNames.NumEmergencyMeetings);
        VanillaGameSettings.EmergencyCooldown = OptionIntItem.Create(6, TBRTabs.GameSettings, "Setting.EmergencyCooldown", (0, 180, 5), 20, ("", "s"), vanillaOption: Int32OptionNames.EmergencyCooldown);
        VanillaGameSettings.DiscussionTime = OptionIntItem.Create(7, TBRTabs.GameSettings, "Setting.DiscussionTime", (0, 500, 15), 15, ("", "s"), vanillaOption: Int32OptionNames.DiscussionTime);
        VanillaGameSettings.VotingTime = OptionIntItem.Create(8, TBRTabs.GameSettings, "Setting.VotingTime", (0, 500, 10), 120, ("", "s"), vanillaOption: Int32OptionNames.VotingTime);
        VanillaGameSettings.AnonymousVotes = OptionCheckboxItem.Create(9, TBRTabs.GameSettings, "Setting.AnonymousVotes", true, vanillaOption: BoolOptionNames.AnonymousVotes);
        VanillaGameSettings.ConfirmEjects = OptionCheckboxItem.Create(10, TBRTabs.GameSettings, "Setting.ConfirmEjects", false);

        OptionHeaderItem.Create(TBRTabs.GameSettings, "Setting.Title.TaskSettings");

        VanillaGameSettings.TaskBarUpdate = OptionStringItem.Create(11, TBRTabs.GameSettings, "Setting.TaskBarUpdate",
            ["Setting.TaskBarUpdate.Always", "Setting.TaskBarUpdate.Meetings", "Setting.TaskBarUpdate.Never"], 1, vanillaOption: Int32OptionNames.TaskBarMode);

        VanillaGameSettings.CommonTasksNum = OptionIntItem.Create(12, TBRTabs.GameSettings, "Role.Option.CommonTasks", (0, 5, 1), 2);
        VanillaGameSettings.LongTasksNum = OptionIntItem.Create(13, TBRTabs.GameSettings, "Role.Option.LongTasks", (0, 5, 1), 2);
        VanillaGameSettings.ShortTasksNum = OptionIntItem.Create(14, TBRTabs.GameSettings, "Role.Option.ShortTasks", (0, 5, 1), 4);
        VanillaGameSettings.VisualTask = OptionCheckboxItem.Create(15, TBRTabs.GameSettings, "Setting.VisualTask", false, vanillaOption: BoolOptionNames.VisualTasks);

        OptionHeaderItem.Create(TBRTabs.GameSettings, Translator.GetString("Setting.Title.SabotageSettings").ToColor(Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Impostor)));
        TBRGameSettings.CamouflageComms = OptionCheckboxItem.Create(16, TBRTabs.GameSettings, "Setting.CamouflageComms", false);
    }

    internal static void SetUpModSettings(bool IsPreload, bool isSync)
    {
        OptionHeaderItem.Create(TBRTabs.SystemSettings, "Setting.Title.ModSettings");
        TBRGameSettings.Presets = OptionPresetItem.Create();
        TBRGameSettings.Algorithm = OptionStringItem.Create(17, TBRTabs.SystemSettings, "Setting.Algorithm",
        ["Default", "NetRandomWrapper", "HashRandomWrapper", "Xorshift", "MersenneTwister"], 0);
        IRandom.SetInstanceById(TBRGameSettings.Algorithm.GetValue());
        TBRGameSettings.Algorithm.OnValueChangeAction = (opt) =>
        {
            IRandom.SetInstanceById(opt.GetStringValue());
        };
        TBRGameSettings.NoGameEnd = OptionCheckboxItem.Create(18, TBRTabs.SystemSettings, Translator.GetString("Setting.NoGameEnd").ToColor("#ff0400"), false);
        TBRGameSettings.Debugging = OptionCheckboxItem.Create(19, TBRTabs.SystemSettings, Translator.GetString("Setting.Debugging").ToColor("#3298FF"), false);

        OptionHeaderItem.Create(TBRTabs.SystemSettings, "Setting.Title.GuessSettings");
        TBRGameSettings.OnlyShowEnabledRoles = OptionCheckboxItem.Create(20, TBRTabs.SystemSettings, "Setting.OnlyShowEnabledRoles", false);
        TBRGameSettings.CrewmatesCanGuess = OptionCheckboxItem.Create(21, TBRTabs.SystemSettings, "Setting.CrewmatesCanGuess", false);
        TBRGameSettings.ImpostorsCanGuess = OptionCheckboxItem.Create(22, TBRTabs.SystemSettings, "Setting.ImpostorsCanGuess", false);
        TBRGameSettings.BenignNeutralsCanGuess = OptionCheckboxItem.Create(23, TBRTabs.SystemSettings, "Setting.BenignNeutralsCanGuess", false);
        TBRGameSettings.KillingNeutralsCanGuess = OptionCheckboxItem.Create(24, TBRTabs.SystemSettings, "Setting.KillingNeutralsCanGuess", false);
        TBRGameSettings.CanGuessAddons = OptionCheckboxItem.Create(25, TBRTabs.SystemSettings, "Setting.CanGuessAddons", false);
    }

    internal static void SetUpRoleSettings(bool IsPreload, bool isSync)
    {
        OptionHeaderItem.Create(TBRTabs.ImpostorRoles, "Setting.Title.ImpostorSettings");
        TBRGameSettings.ImpostorAmount = OptionIntItem.Create(26, TBRTabs.ImpostorRoles, "Setting.Impostors", (0, 5, 1), 2);

        OptionHeaderItem.Create(TBRTabs.NeutralRoles, "Setting.Title.NeutralSettings");
        TBRGameSettings.MaximumBenignNeutralAmount = OptionIntItem.Create(27, TBRTabs.NeutralRoles, "Setting.MaximumBenignNeutralAmount", (0, 5, 1), 2);
        TBRGameSettings.MinimumBenignNeutralAmount = OptionIntItem.Create(28, TBRTabs.NeutralRoles, "Setting.MinimumBenignNeutralAmount", (0, 5, 1), 0);
        TBRGameSettings.MaximumKillingNeutralAmount = OptionIntItem.Create(29, TBRTabs.NeutralRoles, "Setting.MaximumKillingNeutralAmount", (0, 5, 1), 2);
        TBRGameSettings.MinimumKillingNeutralAmount = OptionIntItem.Create(30, TBRTabs.NeutralRoles, "Setting.MinimumKillingNeutralAmount", (0, 5, 1), 0);

        OptionHeaderItem.Create(TBRTabs.ApocalypseRoles, "Setting.Title.ApocalypseSettings");
        TBRGameSettings.MaximumApocalypseAmount = OptionIntItem.Create(40, TBRTabs.ApocalypseRoles, "Setting.MaximumApocalypseAmount", (0, 5, 1), 2);
        TBRGameSettings.MinimumApocalypseAmount = OptionIntItem.Create(41, TBRTabs.ApocalypseRoles, "Setting.MinimumApocalypseAmount", (0, 5, 1), 0);

        OptionHeaderItem.Create(TBRTabs.Addons, "Setting.Title.AddonSettings");
        TBRGameSettings.MaximumAddonAmount = OptionIntItem.Create(31, TBRTabs.Addons, "Setting.MaximumAddonAmount", (0, 5, 1), 2);
        TBRGameSettings.MinimumAddonAmount = OptionIntItem.Create(32, TBRTabs.Addons, "Setting.MinimumAddonAmount", (0, 5, 1), 0);

        if (IsPreload)
        {
            foreach (var role in CustomRoleManager.RolePrefabs)
            {
                role.LoadSettings();
            }
        }
        else
        {
            var roleCategories = new[]
            {
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.Vanilla"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Vanilla ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.Experimental").ToColor("#4FAFFF"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Experimental ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorInformation").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Information ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorBenign").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Benign ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorEvil").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Evil ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorKilling").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Killing ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorSupport").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Support ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorChaos").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Chaos ),
            ( Tab : TBRTabs.ImpostorRoles, Title : Translator.GetString("Setting.RoleCategory.ImpostorGhost").ToColor("#FF2200"), Team : RoleClassTeam.Impostor, Category : RoleClassCategory.Ghost ),

            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.Vanilla"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Vanilla ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.Experimental").ToColor("#4FAFFF"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Experimental ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateInformation").ToColor("#FFDA00"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Information ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateBenign").ToColor("#BFBFBF"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Benign ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateKilling").ToColor("#0F9521"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Killing ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateSupport").ToColor("#0F9521"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Support ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateChaos").ToColor("#FF84DE"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Chaos ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateGhost").ToColor("#B784FF"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Ghost ),
            ( Tab : TBRTabs.CrewmateRoles, Title : Translator.GetString("Setting.RoleCategory.CrewmateEvil").ToColor("#FF2200"), Team : RoleClassTeam.Crewmate, Category : RoleClassCategory.Evil ),

            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.Experimental").ToColor("#4FAFFF"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Experimental ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralInformation").ToColor("#FFDA00"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Information ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralBenign").ToColor("#BFBFBF"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Benign ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralEvil").ToColor("#FF2200"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Evil ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralKilling").ToColor("#FF2200"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Killing ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralSupport").ToColor("#0F9521"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Support ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralChaos").ToColor("#FF84DE"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Chaos ),
            ( Tab : TBRTabs.NeutralRoles, Title : Translator.GetString("Setting.RoleCategory.NeutralGhost").ToColor("#B784FF"), Team : RoleClassTeam.Neutral, Category : RoleClassCategory.Ghost ),

            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.Experimental").ToColor("#4FAFFF"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Experimental ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseInformation").ToColor("#FFDA00"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Information ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseBenign").ToColor("#BFBFBF"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Benign ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseEvil").ToColor("#FF2200"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Evil ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseKilling").ToColor("#FF2200"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Killing ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseSupport").ToColor("#0F9521"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Support ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseChaos").ToColor("#FF84DE"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Chaos ),
            ( Tab : TBRTabs.ApocalypseRoles, Title : Translator.GetString("Setting.RoleCategory.ApocalypseGhost").ToColor("#B784FF"), Team : RoleClassTeam.Apocalypse, Category : RoleClassCategory.Ghost ),

            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.ExperimentalAddon").ToColor("#4FAFFF"), Team : RoleClassTeam.None, Category : RoleClassCategory.Experimental ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.GeneralAddon"), Team : RoleClassTeam.None, Category : RoleClassCategory.GeneralAddon ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.AbilityAddon").ToColor("#30FFCD"), Team : RoleClassTeam.None, Category : RoleClassCategory.AbilityAddon ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.GoodAddon").ToColor("#60FF30"), Team : RoleClassTeam.None, Category : RoleClassCategory.GoodAddon ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.EvilAddon").ToColor("#FF3530"), Team : RoleClassTeam.None, Category : RoleClassCategory.EvilAddon ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.HelpfulAddon").ToColor("#60FF30"), Team : RoleClassTeam.None, Category : RoleClassCategory.HelpfulAddon ),
            ( Tab : TBRTabs.Addons, Title : Translator.GetString("Setting.RoleCategory.HarmfulAddon").ToColor("#FF3530"), Team : RoleClassTeam.None, Category : RoleClassCategory.HarmfulAddon ),
         };

            foreach (var roleCategory in roleCategories)
            {
                int num = 0;
                var Roles = CustomRoleManager.RolePrefabs.Where(r => r.RoleTeam == roleCategory.Team && r.RoleCategory == roleCategory.Category && r.CanBeAssigned).OrderBy(role => role.RoleName);

                if (Roles.Any())
                {
                    OptionHeaderItem.Create(roleCategory.Tab, roleCategory.Title);

                    foreach (var role in Roles)
                    {
                        if (num > 0) OptionDividerItem.Create(roleCategory.Tab);
                        role.LoadSettings();
                        num++;
                    }
                }
            }
        }
    }

    private static int GeneratedId(int @int)
    {
        var num = 100 * @int;
        return num;
    }

    [HarmonyPatch(nameof(GameSettingMenu.Update))]
    [HarmonyPostfix]
    private static void Update_Postfix(GameSettingMenu __instance)
    {
        foreach (OptionTab tab in OptionTab.AllTabs)
        {
            if (tab.TabButton != null)
            {
                tab.TabButton.buttonText.SetText(tab.Name);

                if (!tab.TabButton.selected && !tab.TabButton.activeSprites.active)
                {
                    tab.TabButton.buttonText.color = tab?.Color ?? Color.white;
                }
                else
                {
                    tab.TabButton.buttonText.color = tab.Color + new Color(0.25f, 0.25f, 0.25f);
                }
            }
        }
    }

    [HarmonyPatch(nameof(GameSettingMenu.Start))]
    [HarmonyPrefix]
    private static bool Start_Prefix(GameSettingMenu __instance)
    {
        if (GameSettingMenu.Instance != null && GameSettingMenu.Instance != __instance)
        {
            UnityEngine.Object.Destroy(__instance.gameObject);
        }

        GameSettingMenu.Instance = __instance;
        __instance.transform.localPosition = new Vector3(0f, 0.25f, -300f);
        __instance.MenuDescriptionText.transform.parent.localPosition = new Vector3(0.7834f, -0.3788f, -1f);
        __instance.MenuDescriptionText.transform.parent.localScale = new Vector3(1.35f, 1.35f, 1f);
        __instance.MenuDescriptionText.transform.parent.Find("InfoImage").gameObject.DestroyObj();
        __instance.MenuDescriptionText.transform.localPosition = new Vector3(-3.045f, 0.62f, -2f);
        __instance.MenuDescriptionText.fontSizeMin = 0.85f;

        return false;
    }

    [HarmonyPatch(nameof(GameSettingMenu.Update))]
    [HarmonyPrefix]
    private static bool Update_Prefix(GameSettingMenu __instance)
    {
        if (Controller.currentTouchType != Controller.TouchType.Joystick)
        {
            __instance.ToggleLeftSideDarkener(false);
            __instance.ToggleRightSideDarkener(false);
        }

        return false;
    }

    [HarmonyPatch(nameof(GameSettingMenu.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(GameSettingMenu __instance)
    {
        __instance.gameObject.transform.SetLocalY(-0.1f);
        GameObject PanelSprite = __instance.gameObject.transform.Find("PanelSprite").gameObject;
        if (PanelSprite != null)
        {
            PanelSprite.transform.SetLocalY(-0.32f);
            PanelSprite.transform.localScale = new Vector3(PanelSprite.transform.localScale.x, 0.625f);
        }

        __instance.MenuDescriptionText?.DestroyTextTranslators();
        __instance.PresetsTab?.DestroyObj();
        __instance.RoleSettingsTab?.DestroyObj();
        __instance.GamePresetsButton?.DestroyObj();
        __instance.RoleSettingsButton?.DestroyObj();
        __instance.GameSettingsButton?.gameObject?.SetActive(false);

        SetupSettings();
        GameSettingMenu.Instance.ChangeTab(1, false);
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab))]
    [HarmonyPrefix]
    private static bool ChangeTab_Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        ActiveTab = tabNum;

        // Ensure all tabs and buttons are deactivated first
        foreach (var tab in OptionTab.AllTabs)
        {
            if (tab?.AUTab == null || tab.TabButton == null) continue;

            tab.AUTab.gameObject.SetActive(false);
            tab.TabButton.SelectButton(false);
        }

        // Proceed with setting active tab if previewOnly conditions are met
        if (previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick || !previewOnly)
        {
            var currentTab = OptionTab.GetTabById(tabNum);
            if (currentTab != null && currentTab.AUTab != null && currentTab.TabButton != null)
            {
                currentTab.TabButton.SelectButton(true);

                if (__instance?.MenuDescriptionText != null)
                {
                    __instance.MenuDescriptionText.text = currentTab.Description;
                }

                currentTab.UpdateVisuals();
            }
        }

        return false;
    }
}

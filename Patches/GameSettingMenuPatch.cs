using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

class VanillaGameSettings
{
    public static TBROptionFloatItem? ImpostorVision;
    public static TBROptionFloatItem? KillCooldown;
    public static TBROptionStringItem? KillDistance;

    public static TBROptionFloatItem? PlayerVision;
    public static TBROptionFloatItem? PlayerSpeed;

    public static TBROptionIntItem? EmergencyMeetings;
    public static TBROptionIntItem? EmergencyCooldown;
    public static TBROptionIntItem? DiscussionTime;
    public static TBROptionIntItem? VotingTime;
    public static TBROptionCheckboxItem? AnonymousVotes;
    public static TBROptionCheckboxItem? ConfirmEjects;

    public static TBROptionStringItem? TaskBarUpdate;
    public static TBROptionItem? CommonTasksNum;
    public static TBROptionItem? LongTasksNum;
    public static TBROptionItem? ShortTasksNum;
    public static TBROptionCheckboxItem? VisualTask;
}

class TBRGameSettings
{
    public static TBROptionItem? Algorithm;
    public static TBROptionItem? NoGameEnd;
    public static TBROptionItem? ImpostorAmount;
    public static TBROptionItem? MaximumBenignNeutralAmount;
    public static TBROptionItem? MinimumBenignNeutralAmount;
    public static TBROptionItem? MaximumKillingNeutralAmount;
    public static TBROptionItem? MinimumKillingNeutralAmount;
    public static TBROptionItem? MaximumAddonAmount;
    public static TBROptionItem? MinimumAddonAmount;
    public static TBROptionItem? OnlyShowEnabledRoles;
    public static TBROptionItem? CrewmatesCanGuess;
    public static TBROptionItem? ImpostorsCanGuess;
    public static TBROptionItem? BenignNeutralsCanGuess;
    public static TBROptionItem? KillingNeutralsCanGuess;
    public static TBROptionItem? CanGuessAddons;
    public static TBROptionItem? CamouflageComms;
}

class BetterGameSettingsTemp
{
}

class BetterTabs
{
    public static TBROptionTab? GameSettings;
    public static TBROptionTab? SystemSettings;
    public static TBROptionTab? CrewmateRoles;
    public static TBROptionTab? ImpostorRoles;
    public static TBROptionTab? NeutralRoles;
    public static TBROptionTab? Addons;
}

[HarmonyPatch(typeof(GameSettingMenu))]
static class GameSettingMenuPatch
{
    public static List<TBROptionTab> Tabs = [];
    private static List<TBROptionItem> TitleList = [];
    public static int ActiveTab = 0;
    public static bool Preload = false;

    public static void SetupSettings(bool IsPreload = false, bool isSync = false)
    {
        if (GameSettingMenu.Instance != null && IsPreload && isSync)
        {
            GameSettingMenu.Instance.Close();
        }

        Preload = IsPreload;
        TBROptionItem.IdNum = 0;
        TBROptionItem.BetterOptionItems.Clear();
        TBROptionTab.allTabs.Clear();
        TBROptionItem.TempPlayerOptionDataNum = 0;
        TitleList.Clear();

        bool mapFlag = GameState.IsLobby && GameState.IsHost && !GameState.IsFreePlay;

        BetterTabs.GameSettings = new TBROptionTab().CreateTab(1, Translator.GetString("BetterSetting.Tab.GameSettings"),
            Translator.GetString("BetterSetting.Description.GameSettings"), Color.green, mapFlag);

        BetterTabs.SystemSettings = new TBROptionTab().CreateTab(2, Translator.GetString("BetterSetting.Tab.SystemSettings"),
            Translator.GetString("BetterSetting.Description.SystemSettings"), Color.yellow);
        BetterTabs.CrewmateRoles = new TBROptionTab().CreateTab(3, Translator.GetString("BetterSetting.Tab.CrewmateRoles"),
            Translator.GetString("BetterSetting.Description.CrewmateRoles"), Color.cyan);
        BetterTabs.ImpostorRoles = new TBROptionTab().CreateTab(4, Translator.GetString("BetterSetting.Tab.ImpostorRoles"),
            Translator.GetString("BetterSetting.Description.ImpostorRoles"), Color.red);
        BetterTabs.NeutralRoles = new TBROptionTab().CreateTab(5, Translator.GetString("BetterSetting.Tab.NeutralRoles"),
            Translator.GetString("BetterSetting.Description.NeutralRoles"), Color.gray);
        BetterTabs.Addons = new TBROptionTab().CreateTab(6, Translator.GetString("BetterSetting.Tab.Addons"),
            Translator.GetString("BetterSetting.Description.Addons"), Color.magenta);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.PlayerSettings"), !mapFlag ? 0.1f : 1.5f));

        TitleList.Add(new TBROptionTitleItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.PlayerSettings.Impostor").SetColor(Utils.GetCustomRoleTeamColor(CustomRoleTeam.Impostor))));
        VanillaGameSettings.ImpostorVision = new TBROptionFloatItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.ImpostorVision"), [0.25f, 5f, 0.25f], 1.25f, "", "x", vanillaOption: FloatOptionNames.ImpostorLightMod);
        VanillaGameSettings.KillCooldown = new TBROptionFloatItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.KillCooldown"), [0f, 180f, 2.5f], 25f, "", "s", vanillaOption: FloatOptionNames.KillCooldown);
        VanillaGameSettings.KillDistance = new TBROptionStringItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.KillDistance"),
                [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, vanillaOption: Int32OptionNames.KillDistance);

        new TBROptionDividerItem().Create(BetterTabs.GameSettings);
        TitleList.Add(new TBROptionTitleItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.PlayerSettings.Crewmate").SetColor(Utils.GetCustomRoleTeamColor(CustomRoleTeam.Crewmate))));
        VanillaGameSettings.PlayerVision = new TBROptionFloatItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.PlayerVision"), [0.25f, 5f, 0.25f], 0.75f, "", "x", vanillaOption: FloatOptionNames.CrewLightMod);
        VanillaGameSettings.PlayerSpeed = new TBROptionFloatItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.PlayerSpeed"), [0.25f, 5f, 0.25f], 1.25f, "", "x", vanillaOption: FloatOptionNames.PlayerSpeedMod);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.MeetingSettings")));

        VanillaGameSettings.EmergencyMeetings = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.EmergencyMeetings"), [0, 100, 1], 1, "", "", vanillaOption: Int32OptionNames.NumEmergencyMeetings);
        VanillaGameSettings.EmergencyCooldown = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.EmergencyCooldown"), [0, 180, 5], 20, "", "s", vanillaOption: Int32OptionNames.EmergencyCooldown);
        VanillaGameSettings.DiscussionTime = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.DiscussionTime"), [0, 500, 15], 15, "", "s", vanillaOption: Int32OptionNames.DiscussionTime);
        VanillaGameSettings.VotingTime = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.VotingTime"), [0, 500, 10], 120, "", "s", vanillaOption: Int32OptionNames.VotingTime);
        VanillaGameSettings.AnonymousVotes = new TBROptionCheckboxItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.AnonymousVotes"), true, vanillaOption: BoolOptionNames.AnonymousVotes);
        VanillaGameSettings.ConfirmEjects = new TBROptionCheckboxItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.ConfirmEjects"), false);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.TaskSettings")));

        VanillaGameSettings.TaskBarUpdate = new TBROptionStringItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.TaskBarUpdate"),
            [Translator.GetString("BetterSetting.TaskBarUpdate.Always"), Translator.GetString("BetterSetting.TaskBarUpdate.Meetings"), Translator.GetString("BetterSetting.TaskBarUpdate.Never")], 1, vanillaOption: Int32OptionNames.TaskBarMode);

        VanillaGameSettings.CommonTasksNum = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("Role.Option.CommonTasks"), [0, 5, 1], 2);
        VanillaGameSettings.LongTasksNum = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("Role.Option.LongTasks"), [0, 5, 1], 2);
        VanillaGameSettings.ShortTasksNum = new TBROptionIntItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("Role.Option.ShortTasks"), [0, 5, 1], 4);
        VanillaGameSettings.VisualTask = new TBROptionCheckboxItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.VisualTask"), false, vanillaOption: BoolOptionNames.VisualTasks);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.GameSettings, Translator.GetString("BetterSetting.Title.SabotageSettings").SetColor(Utils.GetCustomRoleTeamColor(CustomRoleTeam.Impostor))));
        TBRGameSettings.CamouflageComms = new TBROptionCheckboxItem().Create(-1, BetterTabs.GameSettings, Translator.GetString("BetterSetting.CamouflageComms"), false);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.SystemSettings, Translator.GetString("BetterSetting.Title.ModSettings")));
        new TBROptionPresetItem().Create(BetterTabs.SystemSettings, 1);
        TBRGameSettings.Algorithm = new TBROptionStringItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.Algorithm"),
        ["Default", "NetRandomWrapper", "HashRandomWrapper", "Xorshift", "MersenneTwister"], 0);
        IRandom.SetInstanceById(TBRGameSettings.Algorithm.GetValue());
        TBRGameSettings.Algorithm.OnValueChange = (TBROptionItem opt) =>
        {
            IRandom.SetInstanceById(opt.GetValue());
        };
        TBRGameSettings.NoGameEnd = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, $"<#ff0400>{Translator.GetString("BetterSetting.NoGameEnd")}</color>", false);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.SystemSettings, Translator.GetString("BetterSetting.Title.GuessSettings")));
        TBRGameSettings.OnlyShowEnabledRoles = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.OnlyShowEnabledRoles"), false);
        TBRGameSettings.CrewmatesCanGuess = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.CrewmatesCanGuess"), false);
        TBRGameSettings.ImpostorsCanGuess = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.ImpostorsCanGuess"), false);
        TBRGameSettings.BenignNeutralsCanGuess = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.BenignNeutralsCanGuess"), false);
        TBRGameSettings.KillingNeutralsCanGuess = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.KillingNeutralsCanGuess"), false);
        TBRGameSettings.CanGuessAddons = new TBROptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.CanGuessAddons"), false);

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.ImpostorRoles, Translator.GetString("BetterSetting.Title.ImpostorSettings")));
        TBRGameSettings.ImpostorAmount = new TBROptionIntItem().Create(-1, BetterTabs.ImpostorRoles, Translator.GetString("BetterSetting.Impostors"), [0, 5, 1], 2, "", "");

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.Title.NeutralSettings")));
        TBRGameSettings.MaximumBenignNeutralAmount = new TBROptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MaximumBenignNeutralAmount"), [0, 5, 1], 2, "", "");
        TBRGameSettings.MinimumBenignNeutralAmount = new TBROptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MinimumBenignNeutralAmount"), [0, 5, 1], 0, "", "");
        TBRGameSettings.MaximumKillingNeutralAmount = new TBROptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MaximumKillingNeutralAmount"), [0, 5, 1], 2, "", "");
        TBRGameSettings.MinimumKillingNeutralAmount = new TBROptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MinimumKillingNeutralAmount"), [0, 5, 1], 0, "", "");

        TitleList.Add(new TBROptionHeaderItem().Create(BetterTabs.Addons, Translator.GetString("BetterSetting.Title.AddonSettings")));
        TBRGameSettings.MaximumAddonAmount = new TBROptionIntItem().Create(-1, BetterTabs.Addons, Translator.GetString("BetterSetting.MaximumAddonAmount"), [0, 5, 1], 2, "", "");
        TBRGameSettings.MinimumAddonAmount = new TBROptionIntItem().Create(-1, BetterTabs.Addons, Translator.GetString("BetterSetting.MinimumAddonAmount"), [0, 5, 1], 0, "", "");

        if (IsPreload)
        {
            foreach (var role in CustomRoleManager.allRoles)
            {
                role.LoadSettings();
            }
        }
        else
        {
            var roleCategories = new[]
            {
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.Vanilla"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Vanilla },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorInformation"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Information },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorBenign"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Benign },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorEvil"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Evil },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorKilling"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Killing },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorSupport"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Support },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorChaos"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Chaos },
                new { Tab = BetterTabs.ImpostorRoles, Title = Translator.GetString("BetterSetting.RoleCategory.ImpostorGhost"), Team = CustomRoleTeam.Impostor, Category = CustomRoleCategory.Ghost },

                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.Vanilla"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Vanilla },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateInformation"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Information },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateBenign"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Benign },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateEvil"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Evil },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateKilling"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Killing },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateSupport"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Support },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateChaos"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Chaos },
                new { Tab = BetterTabs.CrewmateRoles, Title = Translator.GetString("BetterSetting.RoleCategory.CrewmateGhost"), Team = CustomRoleTeam.Crewmate, Category = CustomRoleCategory.Ghost },

                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralInformation"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Information },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralBenign"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Benign },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralEvil"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Evil },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralKilling"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Killing },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralSupport"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Support },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralChaos"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Chaos },
                new { Tab = BetterTabs.NeutralRoles, Title = Translator.GetString("BetterSetting.RoleCategory.NeutralGhost"), Team = CustomRoleTeam.Neutral, Category = CustomRoleCategory.Ghost },

                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.GeneralAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.GeneralAddon },
                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.AbilityAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.AbilityAddon },
                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.GoodAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.GoodAddon },
                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.EvilAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.EvilAddon },
                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.HelpfulAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.HelpfulAddon },
                new { Tab = BetterTabs.Addons, Title = Translator.GetString("BetterSetting.RoleCategory.HarmfulAddon"), Team = CustomRoleTeam.None, Category = CustomRoleCategory.HarmfulAddon },
             };

            foreach (var roleCategory in roleCategories)
            {
                int num = 0;
                var Roles = CustomRoleManager.allRoles.Where(r => r.RoleTeam == roleCategory.Team && r.RoleCategory == roleCategory.Category && r.CanBeAssigned);

                if (Roles.Any())
                {
                    TitleList.Add(new TBROptionHeaderItem().Create(roleCategory.Tab, roleCategory.Title));

                    foreach (var role in Roles)
                    {
                        if (num > 0) new TBROptionDividerItem().Create(roleCategory.Tab);
                        role.LoadSettings();
                        num++;
                    }
                }
            }
        }

        Preload = false;
    }

    private static void Initialize()
    {
        TBROptionItem.UpdatePositions();
    }

    [HarmonyPatch(nameof(GameSettingMenu.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(GameSettingMenu __instance)
    {
        foreach (TBROptionTab tab in TBROptionTab.allTabs)
        {
            if (tab.TabButton != null)
            {
                tab.TabButton.buttonText.SetText(tab.Name);

                if (!tab.TabButton.selected && !tab.TabButton.activeSprites.active)
                {
                    tab.TabButton.buttonText.color = (Color)tab.Color;
                }
                else
                {
                    tab.TabButton.buttonText.color = (Color)tab.Color + new Color(0.25f, 0.25f, 0.25f);
                }
            }
        }
    }

    [HarmonyPatch(nameof(GameSettingMenu.Start))]
    [HarmonyPrefix]
    public static bool Start_Prefix(GameSettingMenu __instance)
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
    public static bool Update_Prefix(GameSettingMenu __instance)
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
    public static void Start_Postfix(GameSettingMenu __instance)
    {
        __instance.gameObject.transform.SetLocalY(-0.1f);
        GameObject PanelSprite = __instance.gameObject.transform.Find("PanelSprite").gameObject;
        if (PanelSprite != null)
        {
            PanelSprite.transform.SetLocalY(-0.32f);
            PanelSprite.transform.localScale = new Vector3(PanelSprite.transform.localScale.x, 0.625f);
        }

        __instance.MenuDescriptionText?.DestroyTextTranslator();
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
    public static bool ChangeTab_Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        ActiveTab = tabNum;

        // Ensure all tabs and buttons are deactivated first
        foreach (var tab in TBROptionTab.allTabs)
        {
            if (tab?.Tab == null || tab.TabButton == null) continue;

            tab.Tab.gameObject.SetActive(false);
            tab.TabButton.SelectButton(false);
        }

        // Proceed with setting active tab if previewOnly conditions are met
        if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
        {
            var currentTab = TBROptionTab.allTabs.FirstOrDefault(t => t.Id == tabNum);
            if (currentTab != null && currentTab.Tab != null && currentTab.TabButton != null)
            {
                currentTab.Tab.gameObject.SetActive(true);
                currentTab.TabButton.SelectButton(true);

                if (__instance?.MenuDescriptionText != null)
                {
                    __instance.MenuDescriptionText.text = currentTab.Description;
                }
                Initialize();
            }
        }

        return false;
    }
}

// Preload modified vanilla options
[HarmonyPatch(typeof(GameOptionsManager))]
static class GameOptionsManagerPatch
{
    [HarmonyPatch(nameof(GameOptionsManager.Initialize))]
    [HarmonyPostfix]
    public static void CreateSettings_Postfix(/*GameOptionsManager __instance*/)
    {
        Main.CurrentOptions.SetInt(Int32OptionNames.RulePreset, 100);
        Main.CurrentOptions.SetBool(BoolOptionNames.IsDefaults, false);

        foreach (var Option in TBROptionItem.BetterOptionItems)
        {
            if (Option.TryCast<TBROptionCheckboxItem>(out var Bool))
            {
                if (Bool.VanillaOption != null)
                {
                    Bool.Load(Bool.defaultValue);
                }
            }
            else if (Option.TryCast<TBROptionFloatItem>(out var Float))
            {
                if (Float.VanillaOption != null)
                {
                    Float.Load(Float.defaultValue);
                }
            }
            else if (Option.TryCast<TBROptionIntItem>(out var Int))
            {
                if (Int.VanillaOption != null)
                {
                    Int.Load(Int.defaultValue);
                }
            }
            else if (Option.TryCast<TBROptionStringItem>(out var String))
            {
                if (String.VanillaOption != null)
                {
                    String.Load(String.defaultValue);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(GameOptionsMenu))]
static class GameOptionsMenuPatch
{
    [HarmonyPatch(nameof(GameOptionsMenu.CreateSettings))]
    [HarmonyPrefix]
    public static bool CreateSettings_Prefix(GameOptionsMenu __instance)
    {
        foreach (var tab in TBROptionTab.allTabs)
        {
            if (tab.Tab == __instance)
            {
                return false;
            }
        }

        return true;
    }
}

// Allow settings bypass
[HarmonyPatch(typeof(NumberOption))]
static class NumberOptionPatch
{
    [HarmonyPatch(nameof(NumberOption.Increase))]
    [HarmonyPrefix]
    public static bool Increase_Prefix(NumberOption __instance)
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (__instance.Value + __instance.Increment * times > __instance.ValidRange.max)
        {
            __instance.Value = __instance.ValidRange.max;
        }
        else
        {
            __instance.Value = __instance.ValidRange.Clamp(__instance.Value + __instance.Increment * times);
        }
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();
        return false;
    }

    [HarmonyPatch(nameof(NumberOption.Decrease))]
    [HarmonyPrefix]
    public static bool Decrease_Prefix(NumberOption __instance)
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (__instance.Value - __instance.Increment * times < __instance.ValidRange.min)
        {
            __instance.Value = __instance.ValidRange.min;
        }
        else
        {
            __instance.Value = __instance.ValidRange.Clamp(__instance.Value - __instance.Increment * times);
        }
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();
        return false;
    }
}

[HarmonyPatch(typeof(NotificationPopper))]
static class NotificationPopperPatch
{
    [HarmonyPatch(nameof(NotificationPopper.AddSettingsChangeMessage))]
    [HarmonyPrefix]
    public static bool AddSettingsChangeMessage_Prefix(/*NotificationPopper __instance*/)
    {
        return false;
    }
}
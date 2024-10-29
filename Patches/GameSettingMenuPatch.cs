using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;


class BetterGameSettings
{
    public static BetterOptionItem? ConfirmEjects;
    public static BetterOptionItem? CommonTasksNum;
    public static BetterOptionItem? LongTasksNum;
    public static BetterOptionItem? ShortTasksNum;
    public static BetterOptionItem? ImpostorAmount;
    public static BetterOptionItem? MaximumBenignNeutralAmount;
    public static BetterOptionItem? MinimumBenignNeutralAmount;
    public static BetterOptionItem? MaximumKillingNeutralAmount;
    public static BetterOptionItem? MinimumKillingNeutralAmount;
    public static BetterOptionItem? MaximumAddonAmount;
    public static BetterOptionItem? MinimumAddonAmount;
    public static BetterOptionItem? OnlyShowEnabledRoles;
    public static BetterOptionItem? CrewmatesCanGuess;
    public static BetterOptionItem? ImpostorsCanGuess;
    public static BetterOptionItem? BenignNeutralsCanGuess;
    public static BetterOptionItem? KillingNeutralsCanGuess;
    public static BetterOptionItem? CanGuessAddons;
}

class BetterGameSettingsTemp
{
}

class BetterTabs
{
    public static BetterOptionTab? SystemSettings;
    public static BetterOptionTab? CrewmateRoles;
    public static BetterOptionTab? ImpostorRoles;
    public static BetterOptionTab? NeutralRoles;
    public static BetterOptionTab? Addons;
}

[HarmonyPatch(typeof(GameSettingMenu))]
static class GameSettingMenuPatch
{
    public static List<BetterOptionTab> Tabs = [];
    private static List<BetterOptionItem> TitleList = [];
    public static int ActiveTab = 0;
    public static bool Preload = false;

    public static void SetupSettings(bool IsPreload = false)
    {
        Preload = IsPreload;
        BetterOptionItem.IdNum = 0;
        BetterOptionItem.BetterOptionItems.Clear();
        BetterOptionTab.allTabs.Clear();
        BetterOptionItem.TempPlayerOptionDataNum = 0;
        TitleList.Clear();

        BetterTabs.SystemSettings = new BetterOptionTab().CreateTab(2, Translator.GetString("BetterSetting.Tab.SystemSettings"),
            Translator.GetString("BetterSetting.Description.SystemSettings"), Color.green);
        BetterTabs.CrewmateRoles = new BetterOptionTab().CreateTab(3, Translator.GetString("BetterSetting.Tab.CrewmateRoles"),
            Translator.GetString("BetterSetting.Description.CrewmateRoles"), Color.cyan);
        BetterTabs.ImpostorRoles = new BetterOptionTab().CreateTab(4, Translator.GetString("BetterSetting.Tab.ImpostorRoles"),
            Translator.GetString("BetterSetting.Description.ImpostorRoles"), Color.red);
        BetterTabs.NeutralRoles = new BetterOptionTab().CreateTab(5, Translator.GetString("BetterSetting.Tab.NeutralRoles"),
            Translator.GetString("BetterSetting.Description.NeutralRoles"), Color.gray);
        BetterTabs.Addons = new BetterOptionTab().CreateTab(6, Translator.GetString("BetterSetting.Tab.Addons"),
            Translator.GetString("BetterSetting.Description.Addons"), Color.magenta);

        TitleList.Add(new BetterOptionHeaderItem().Create(BetterTabs.SystemSettings, Translator.GetString("BetterSetting.Title.ModSettings")));
        new BetterOptionPresetItem().Create(BetterTabs.SystemSettings, 1);
        BetterGameSettings.ConfirmEjects = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.ConfirmEjects"), false);
        BetterGameSettings.CommonTasksNum = new BetterOptionIntItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("Role.Option.CommonTasks"), [0, 5, 1], 2);
        BetterGameSettings.LongTasksNum = new BetterOptionIntItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("Role.Option.LongTasks"), [0, 5, 1], 2);
        BetterGameSettings.ShortTasksNum = new BetterOptionIntItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("Role.Option.ShortTasks"), [0, 5, 1], 4);

        TitleList.Add(new BetterOptionHeaderItem().Create(BetterTabs.SystemSettings, Translator.GetString("BetterSetting.Title.GuessSettings")));
        BetterGameSettings.OnlyShowEnabledRoles = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.OnlyShowEnabledRoles"), false);
        BetterGameSettings.CrewmatesCanGuess = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.CrewmatesCanGuess"), false);
        BetterGameSettings.ImpostorsCanGuess = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.ImpostorsCanGuess"), false);
        BetterGameSettings.BenignNeutralsCanGuess = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.BenignNeutralsCanGuess"), false);
        BetterGameSettings.KillingNeutralsCanGuess = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.KillingNeutralsCanGuess"), false);
        BetterGameSettings.CanGuessAddons = new BetterOptionCheckboxItem().Create(-1, BetterTabs.SystemSettings, Translator.GetString("BetterSetting.CanGuessAddons"), false);

        TitleList.Add(new BetterOptionHeaderItem().Create(BetterTabs.ImpostorRoles, Translator.GetString("BetterSetting.Title.ImpostorSettings")));
        BetterGameSettings.ImpostorAmount = new BetterOptionIntItem().Create(-1, BetterTabs.ImpostorRoles, Translator.GetString("BetterSetting.Impostors"), [0, 5, 1], 2, "", "");

        TitleList.Add(new BetterOptionHeaderItem().Create(BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.Title.NeutralSettings")));
        BetterGameSettings.MaximumBenignNeutralAmount = new BetterOptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MaxNonKillingNeutrals"), [0, 5, 1], 2, "", "");
        BetterGameSettings.MinimumBenignNeutralAmount = new BetterOptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MinimumBenignNeutralAmount"), [0, 5, 1], 0, "", "");
        BetterGameSettings.MaximumKillingNeutralAmount = new BetterOptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MaximumKillingNeutralAmount"), [0, 5, 1], 2, "", "");
        BetterGameSettings.MinimumKillingNeutralAmount = new BetterOptionIntItem().Create(-1, BetterTabs.NeutralRoles, Translator.GetString("BetterSetting.MinimumKillingNeutralAmount"), [0, 5, 1], 0, "", "");

        TitleList.Add(new BetterOptionHeaderItem().Create(BetterTabs.Addons, Translator.GetString("BetterSetting.Title.AddonSettings")));
        BetterGameSettings.MaximumAddonAmount = new BetterOptionIntItem().Create(-1, BetterTabs.Addons, Translator.GetString("BetterSetting.MaximumAddonAmount"), [0, 5, 1], 2, "", "");
        BetterGameSettings.MinimumAddonAmount = new BetterOptionIntItem().Create(-1, BetterTabs.Addons, Translator.GetString("BetterSetting.MinimumAddonAmount"), [0, 5, 1], 0, "", "");

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
                    TitleList.Add(new BetterOptionHeaderItem().Create(roleCategory.Tab, roleCategory.Title));

                    foreach (var role in Roles)
                    {
                        if (num > 0) new BetterOptionDividerItem().Create(roleCategory.Tab);
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
        BetterOptionItem.UpdatePositions();
    }

    [HarmonyPatch(nameof(GameSettingMenu.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(GameSettingMenu __instance)
    {
        foreach (BetterOptionTab tab in BetterOptionTab.allTabs)
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

        __instance.MenuDescriptionText.DestroyTextTranslator();
        GameSettingMenu.Instance.ChangeTab(1, false);

        __instance.PresetsTab.DestroyObj();
        __instance.RoleSettingsTab.DestroyObj();
        __instance.GamePresetsButton.DestroyObj();
        __instance.RoleSettingsButton.DestroyObj();
        __instance.GameSettingsButton.OnMouseOver.RemoveAllListeners();
        __instance.GameSettingsButton.transform.position = __instance.GameSettingsButton.transform.position + new Vector3(0f, 0.6f, 0f);

        SetupSettings();
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab))]
    [HarmonyPrefix]
    public static bool ChangeTab_Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
    {
        ActiveTab = tabNum;
        __instance.GameSettingsTab.gameObject.SetActive(false);
        __instance.GameSettingsButton.SelectButton(false);

        foreach (var tab in BetterOptionTab.allTabs)
        {
            if (tab.Tab == null || tab.TabButton == null) continue;

            tab.Tab.gameObject.SetActive(false);
            tab.TabButton.SelectButton(false);
        }

        if ((previewOnly && Controller.currentTouchType == Controller.TouchType.Joystick) || !previewOnly)
        {
            if (tabNum > 1 && BetterOptionTab.allTabs.FirstOrDefault(t => t.Id == tabNum).Tab != null
                && BetterOptionTab.allTabs.FirstOrDefault(t => t.Id == tabNum).TabButton != null)
            {
                var tab = BetterOptionTab.allTabs.FirstOrDefault(t => t.Id == tabNum);
                tab.Tab.gameObject.SetActive(true);
                tab.TabButton.SelectButton(true);
                __instance.MenuDescriptionText.text = tab.Description;
                Initialize();
            }
            else if (tabNum == 1)
            {
                __instance.GameSettingsTab.gameObject.SetActive(true);
                __instance.GameSettingsButton.SelectButton(true);
                __instance.MenuDescriptionText.text = Translator.GetString(StringNames.GameSettingsDescription);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(GameOptionsMenu))]
static class GameOptionsMenuPatch
{
    [HarmonyPatch(nameof(GameOptionsMenu.CreateSettings))]
    [HarmonyPrefix]
    public static bool CreateSettings_Prefix(GameOptionsMenu __instance)
    {
        foreach (var tab in BetterOptionTab.allTabs)
        {
            if (tab.Tab == __instance)
            {
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(OptionsConsole))]
static class OptionsConsolePatch
{
    [HarmonyPatch(nameof(OptionsConsole.CanUse))]
    [HarmonyPrefix]
    public static bool CanUse_Prefix(OptionsConsole __instance, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (__instance != null)
        {
            __instance.HostOnly = false;
            if (PlayerControl.LocalPlayer?.BetterData()?.HasMod == false && !GameState.IsHost)
            {
                couldUse = false;
                canUse = false;
                __result = float.MaxValue;
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
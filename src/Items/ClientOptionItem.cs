using BepInEx.Configuration;
using TheBetterRoles.Modules;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheBetterRoles.Items;

internal class ClientOptionItem
{
    internal ConfigEntry<bool> Config { get; private set; }
    internal ToggleButtonBehaviour ToggleButton { get; private set; }

    internal static SpriteRenderer CustomBackground;
    private static int numOptions;

    private ClientOptionItem(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null,
        Func<bool> toggleCheck = null,
        bool isToggle = true)
    {
        Config = config;
        InitializeBackground(optionsMenuBehaviour);
        CreateOptionButton(name, optionsMenuBehaviour, additionalOnClickAction, toggleCheck, isToggle);
        numOptions++;
    }

    internal static ClientOptionItem Create(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null,
        Func<bool> toggleCheck = null,
        bool isToggle = true)
    {
        toggleCheck ??= () => true;
        return new ClientOptionItem(name, config, optionsMenuBehaviour, additionalOnClickAction, toggleCheck, isToggle);
    }

    private void InitializeBackground(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        if (CustomBackground != null) return;

        numOptions = 0;
        CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
        CustomBackground.name = "CustomBackground";
        CustomBackground.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        CustomBackground.transform.localPosition += Vector3.back * 8;
        CustomBackground.gameObject.SetActive(false);

        var closeButton = CreateCloseButton(optionsMenuBehaviour.DisableMouseMovement);
        closeButton.OnClick = new();
        closeButton.OnClick.AddListener((Action)(() => CustomBackground.gameObject.SetActive(false)));

        AdjustOptionsMenuButtons(optionsMenuBehaviour, closeButton);
    }

    private PassiveButton CreateCloseButton(ToggleButtonBehaviour baseButton)
    {
        var closeButton = Object.Instantiate(baseButton, CustomBackground.transform);
        closeButton.transform.localPosition = new Vector3(1.3f, -2.3f, -6f);
        closeButton.name = "Back";
        closeButton.Text.text = "Back";
        closeButton.Background.color = Palette.DisabledGrey;

        return closeButton.GetComponent<PassiveButton>();
    }

    private void AdjustOptionsMenuButtons(OptionsMenuBehaviour optionsMenuBehaviour, PassiveButton closeButton)
    {
        UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
        PassiveButton leaveButton = null, returnButton = null;

        foreach (var button in selectableButtons)
        {
            if (button == null) continue;

            if (button.name == "LeaveGameButton")
                leaveButton = button.GetComponent<PassiveButton>();
            else if (button.name == "ReturnToGameButton")
                returnButton = button.GetComponent<PassiveButton>();
        }

        var generalTab = optionsMenuBehaviour.DisableMouseMovement.transform.parent.parent.parent;

        var modOptionsButton = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement, generalTab);
        modOptionsButton.transform.localPosition = leaveButton?.transform?.localPosition ?? new Vector3(0f, -2.4f, 1f);
        modOptionsButton.name = "Better Options";
        modOptionsButton.Text.text = Translator.GetString("BetterOption");
        modOptionsButton.Background.color = Colors.HexToColor("#00FFFC").ColorToColor32();
        var modOptionsPassiveButton = modOptionsButton.GetComponent<PassiveButton>();
        modOptionsPassiveButton.OnClick = new();
        modOptionsPassiveButton.OnClick.AddListener((Action)(() => CustomBackground.gameObject.SetActive(true)));

        if (leaveButton != null)
            leaveButton.transform.localPosition = new Vector3(-1.35f, -2.411f, -1f);
        if (returnButton != null)
            returnButton.transform.localPosition = new Vector3(1.35f, -2.411f, -1f);
    }

    private void CreateOptionButton(
        string name,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction,
        Func<bool> toggleCheck,
        bool isToggle)
    {
        ToggleButton = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement, CustomBackground.transform);
        ToggleButton.transform.localPosition = new Vector3(
            numOptions % 2 == 0 ? -1.3f : 1.3f,
            2.2f - 0.5f * (numOptions / 2),
            -6f);
        ToggleButton.name = name;
        ToggleButton.Text.text = name;

        if (isToggle)
            ToggleButton.Text.text += Config?.Value == true ? ": On" : ": Off";

        var passiveButton = ToggleButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new();

        if (!isToggle)
            ConfigureNonToggleButton(name, additionalOnClickAction, toggleCheck, passiveButton);
        else
            ConfigureToggleButton(additionalOnClickAction, toggleCheck, passiveButton);
    }

    private void ConfigureNonToggleButton(
        string name,
        Action additionalOnClickAction,
        Func<bool> toggleCheck,
        PassiveButton passiveButton)
    {
        ToggleButton.Text.text = name;
        ToggleButton.Rollover?.ChangeOutColor(Colors.HexToColor("#00FFFC").ColorToColor32());
        ToggleButton.Text.color = Color.white;

        passiveButton.OnClick.AddListener((Action)(() =>
        {
            if (toggleCheck())
                additionalOnClickAction?.Invoke();
        }));
    }

    private void ConfigureToggleButton(
        Action additionalOnClickAction,
        Func<bool> toggleCheck,
        PassiveButton passiveButton)
    {
        passiveButton.OnClick.AddListener((Action)(() =>
        {
            if (toggleCheck())
            {
                if (Config != null)
                    Config.Value = !Config.Value;
                UpdateToggle();
                additionalOnClickAction?.Invoke();
            }
        }));
        UpdateToggle();
    }

    internal void UpdateToggle()
    {
        if (ToggleButton == null) return;

        var color = Config?.Value == true ? Colors.HexToColor("#00FFFC").ColorToColor32() : Palette.DisabledGrey.ColorToColor32();
        var textColor = Config?.Value == true ? Color.white : new Color(1f, 1f, 1f, 0.5f);

        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
        ToggleButton.Text.color = textColor;
        ToggleButton.Text.text = $"{ToggleButton.name}{(Config?.Value == true ? ": On" : ": Off")}";
    }
}

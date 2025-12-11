using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

/// <summary>
/// Option Tab.
/// </summary>
internal class OptionTab
{
    internal static List<OptionTab> AllTabs = [];

    internal readonly List<OptionItem> Children = [];
    internal int Id { get; private set; }
    internal string? Name => Translator.GetString(TranName);
    internal string? TranName { get; private set; }
    internal string? Description => Translator.GetString(TranDescription);
    internal string? TranDescription { get; private set; }
    internal GameOptionsMenu? AUTab { get; private set; }
    internal PassiveButton? TabButton { get; private set; }
    internal Color Color { get; private set; }

    /// <summary>
    /// Creates a new option tab for the options menu. If an option tab with the specified ID already exists, 
    /// it reuses the existing tab, clears its children, and sets up its behavior.
    /// </summary>
    /// <param name="Id">The unique identifier for the option tab.</param>
    /// <param name="tranName">The translation string for the name of the tab.</param>
    /// <param name="tranDescription">The translation string for the description of the tab.</param>
    /// <param name="Color">The color associated with the tab.</param>
    /// <param name="doNotDestroyMapPicker">A flag indicating whether the map picker should not be destroyed when recreating the tab.</param>
    /// <returns>The created or reused <see cref="OptionTab"/> instance.</returns>
    internal static OptionTab Create(int Id, string tranName, string tranDescription, Color Color, bool doNotDestroyMapPicker = false)
    {
        if (GetTabById(Id) is OptionTab optionTab)
        {
            optionTab.Children.Clear();
            optionTab.CreateBehavior(doNotDestroyMapPicker);
            return optionTab;
        }

        var Item = new OptionTab
        {
            Id = Id,
            TranName = tranName,
            TranDescription = tranDescription,
            Color = Color
        };
        AllTabs.Add(Item);

        Item.CreateBehavior(doNotDestroyMapPicker);
        return Item;
    }

    internal static OptionTab? GetTabById(int id) => AllTabs.FirstOrDefault(tab => tab.Id == id);

    private void CreateBehavior(bool doNotDestroyMapPicker)
    {
        if (GameSettingMenuPatch.Preload) return;

        var SettingsButton = UnityEngine.Object.Instantiate(GameSettingMenu.Instance.GameSettingsButton, GameSettingMenu.Instance.GameSettingsButton.transform.parent);
        TabButton = SettingsButton;

        SettingsButton.gameObject.SetActive(true);
        SettingsButton.name = Name;
        SettingsButton.OnClick.RemoveAllListeners();
        SettingsButton.OnMouseOver.RemoveAllListeners();

        SettingsButton.activeSprites.GetComponent<SpriteRenderer>().color = Color;
        SettingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = Color;
        SettingsButton.selectedSprites.GetComponent<SpriteRenderer>().color = Color;

        SettingsButton.gameObject.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 0.6176f);

        SettingsButton.OnClick.AddListener(new Action(() =>
        {
            GameSettingMenu.Instance.ChangeTab(Id, false);
        }));

        var SettingsTab = UnityEngine.Object.Instantiate(GameSettingMenu.Instance.GameSettingsTab, GameSettingMenu.Instance.GameSettingsTab.transform.parent);
        AUTab = SettingsTab;
        SettingsTab.name = Name;
        if (!doNotDestroyMapPicker) SettingsTab.scrollBar.Inner.DestroyChildren();

        AlignButtons();
        AUTab.gameObject.SetActive(false);
    }

    internal static void AlignButtons()
    {
        int buttonCount = AllTabs.Count;
        float buttonHeight = 0.4f; // The height offset between buttons
        Vector3 startingPosition = new(-3.2f, -0.8f, -2f); // Starting position from GameSettingsButton
        float xOffset = 0.75f; // Horizontal offset for columns

        // Loop through each button and calculate its position
        for (int i = 0; i < buttonCount; i++)
        {
            var tab = AllTabs[i];
            if (tab.TabButton == null) continue;

            // Set the alignment to center
            tab.TabButton.buttonText.alignment = TextAlignmentOptions.Center;

            // Calculate the position for the current button
            int columnIndex = i % 2; // 0 for left column, 1 for right column
            int rowIndex = i / 2; // Which row the button is in

            // Calculate the new position
            float xPosition = startingPosition.x + (columnIndex == 0 ? -xOffset : xOffset); // Adjust x based on column
            float yPosition = startingPosition.y - rowIndex * buttonHeight; // Adjust y based on row

            // Set the position
            tab.TabButton.transform.localPosition = new(xPosition, yPosition, startingPosition.z);

            // Set button scale
            tab.TabButton.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            tab.TabButton.activeSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
            tab.TabButton.inactiveSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
            tab.TabButton.selectedSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
        }
    }

    internal void UpdateVisuals()
    {
        ShowOptions();
    }

    private void ShowOptions()
    {
        if (AUTab == null) return;

        AUTab.gameObject.SetActive(true);
        float spacingNum = 0f;
        foreach (var opt in Children)
        {
            if (opt?.Obj == null) continue;
            if (opt?.Tab.Id != Id || opt.Hide)
            {
                opt.Obj.gameObject.SetActive(false);
                continue;
            }

            opt.Obj.gameObject.SetActive(true);

            spacingNum += opt switch
            {
                OptionHeaderItem headerItem => headerItem.Distance.top,
                OptionTitleItem titleItem => titleItem.Distance.top,
                OptionDividerItem dividerItem => dividerItem.Distance.top,
                _ => 0f,
            };

            if (opt.IsOption)
            {
                opt.Obj.transform.localPosition = new Vector3(1.4f, 2f - 1f * spacingNum, 0f);
            }
            else
            {
                opt.Obj.transform.localPosition = new Vector3(-0.6f, 2f - 1f * spacingNum, 0f);
            }

            spacingNum += opt switch
            {
                OptionHeaderItem headerItem => headerItem.Distance.bottom,
                OptionTitleItem titleItem => titleItem.Distance.bottom,
                OptionDividerItem dividerItem => dividerItem.Distance.bottom,
                _ => 0.45f,
            };

            opt.UpdateVisuals(false);
        }

        AUTab?.scrollBar?.SetYBoundsMax(spacingNum - 2.5f);
        AUTab?.scrollBar?.ScrollRelative(new(0f, 0f));
    }

    internal static void FindOptions(string name)
    {
        throw new NotImplementedException();
    }
}

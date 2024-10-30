using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionTab
{
    public static List<BetterOptionTab> allTabs = [];

    public int Id;
    public string? Name;
    public string? Description;
    public GameOptionsMenu? Tab;
    public PassiveButton? TabButton;
    public Color? Color;

    public BetterOptionTab CreateTab(int Id, string Name, string Description, Color Color, bool destroyMapPicker = true)
    {
        if (GameSettingMenuPatch.Preload)
        {
            if (GameSettingMenuPatch.Tabs.Any(op => op.Id == Id))
            {
                return GameSettingMenuPatch.Tabs.First(op => op.Id == Id);
            }
            else
            {
                return this;
            }
        }

        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Color = Color;

        if (!GameSettingMenu.Instance)
        {
            return this;
        }

        var BetterSettingsButton = UnityEngine.Object.Instantiate(GameSettingMenu.Instance.GameSettingsButton, GameSettingMenu.Instance.GameSettingsButton.transform.parent);
        TabButton = BetterSettingsButton;

        BetterSettingsButton.gameObject.SetActive(true);
        BetterSettingsButton.name = Name;
        BetterSettingsButton.OnClick.RemoveAllListeners();
        BetterSettingsButton.OnMouseOver.RemoveAllListeners();

        BetterSettingsButton.activeSprites.GetComponent<SpriteRenderer>().color = Color;
        BetterSettingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = Color;
        BetterSettingsButton.selectedSprites.GetComponent<SpriteRenderer>().color = Color;

        BetterSettingsButton.gameObject.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 0.6176f);

        BetterSettingsButton.OnClick.AddListener(new Action(() =>
        {
            GameSettingMenu.Instance.ChangeTab(Id, false);
            BetterOptionItem.UpdatePositions();
        }));

        var BetterSettingsTab = UnityEngine.Object.Instantiate(GameSettingMenu.Instance.GameSettingsTab, GameSettingMenu.Instance.GameSettingsTab.transform.parent);
        Tab = BetterSettingsTab;
        BetterSettingsTab.name = Name;
        if (destroyMapPicker) BetterSettingsTab.scrollBar.Inner.DestroyChildren();

        allTabs.Add(this);
        AlignButtons();
        Tab.gameObject.SetActive(false);
        return this;
    }

    public static void AlignButtons()
    {
        int buttonCount = allTabs.Count;
        float buttonHeight = 0.4f; // The height offset between buttons
        Vector3 startingPosition = GameSettingMenu.Instance.GameSettingsButton.transform.position + new Vector3(-0.15f, 0.8f, 0f); // Starting position from GameSettingsButton
        float xOffset = 0.7f; // Horizontal offset for columns

        // Loop through each button and calculate its position
        for (int i = 0; i < buttonCount; i++)
        {
            var tab = allTabs[i];

            // Set the alignment to center
            tab.TabButton.buttonText.alignment = TextAlignmentOptions.Center;

            // Calculate the position for the current button
            int columnIndex = i % 2; // 0 for left column, 1 for right column
            int rowIndex = i / 2; // Which row the button is in

            // Calculate the new position
            float xPosition = startingPosition.x + (columnIndex == 0 ? -xOffset : xOffset); // Adjust x based on column
            float yPosition = startingPosition.y - rowIndex * buttonHeight; // Adjust y based on row

            // Set the position
            tab.TabButton.transform.position = new Vector3(xPosition, yPosition, startingPosition.z);

            // Set button scale
            tab.TabButton.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            tab.TabButton.activeSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
            tab.TabButton.inactiveSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
            tab.TabButton.selectedSprites.transform.localScale = new Vector3(0.8f, 1f, 1f);
        }

        // Center the button if there's only one
        if (buttonCount == 1)
        {
            var singleTab = allTabs[0];
            singleTab.TabButton.transform.position = new Vector3(startingPosition.x, startingPosition.y, startingPosition.z);
        }
    }
}

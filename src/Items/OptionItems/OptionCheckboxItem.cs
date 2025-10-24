using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionCheckboxItem : OptionItem<bool>
{
    internal override bool ShowChildren => base.ShowChildren && MainValue;
    internal BoolOptionNames? VanillaOption { get; set; }

    /// <summary>
    /// Creates a new checkbox item for the options menu. If an item with the specified ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <param name="id">The unique identifier for the checkbox item.</param>
    /// <param name="tab">The tab to which the checkbox item belongs.</param>
    /// <param name="tranStr">The translation string for the checkbox item label.</param>
    /// <param name="defaultValue">The default value (checked/unchecked) for the checkbox.</param>
    /// <param name="parent">An optional parent option item that this checkbox item belongs to.</param>
    /// <param name="vanillaOption">An optional vanilla option name, if any, for this checkbox item.</param>
    /// <returns>The created or reused <see cref="OptionCheckboxItem"/> instance.</returns>
    internal static OptionCheckboxItem Create(int id, OptionTab tab, string tranStr, bool defaultValue, OptionItem parent = null, BoolOptionNames? vanillaOption = null)
    {
        if (GetOptionById(id) is OptionCheckboxItem checkboxItem)
        {
            checkboxItem.CreateBehavior();
            return checkboxItem;
        }

        OptionCheckboxItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.DefaultValue = defaultValue;
        Item.VanillaOption = vanillaOption;

        if (parent != null)
        {
            Item.Parent = parent;
            parent.Children.Add(Item);
        }

        Item.CreateBehavior();
        return Item;
    }

    protected override void CreateBehavior()
    {
        TryLoad();
        if (!GameSettingMenu.Instance) return;
        AllTBROptionsTemp.Add(this);
        var ToggleOption = UnityEngine.Object.Instantiate(Tab.AUTab.checkboxOrigin, Tab.AUTab.settingsContainer);
        Option = ToggleOption;
        Obj = Option.gameObject;
        Option.enabled = false;
        Tab.Children.Add(this);
        TitleTMP = ToggleOption.TitleText;
        SetupText(ToggleOption.TitleText);
        SetupOptionBehavior();
        SetOptionVisuals();
    }

    protected override void SetupOptionBehavior()
    {
        if (Option is ToggleOption toggleOption)
        {
            SetupAUOption(Option);
            toggleOption.DestroyTextTranslators();
            toggleOption.TitleText.text = Name;
            var button = toggleOption.buttons[0];
            button.OnClick = new();
            button.OnClick.AddListener((Action)(() => SetValue(!Value)));
        }
    }

    internal override void SyncAUOption()
    {
        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetBool((BoolOptionNames)VanillaOption, Value);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetBool((BoolOptionNames)VanillaOption, Value);
        }
    }

    internal override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (Option is ToggleOption toggleOption)
        {
            toggleOption.CheckMark.enabled = MainValue;

            if (!GameState.IsHost && GameState.IsInGame)
            {
                toggleOption.CheckMark.transform.parent.Find("ActiveSprite").gameObject.SetActive(false);
                toggleOption.CheckMark.transform.parent.Find("InactiveSprite").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
                toggleOption.CheckMark.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
                toggleOption.transform.Find("Toggle").GetComponent<PassiveButton>().enabled = false;
            }
        }

        if (updateTabVisuals)
        {
            Tab.UpdateVisuals();
        }
    }

    internal override string ValueAsString()
    {
        Color color = MainValue ? Color.green : Color.red;
        string @bool = MainValue ? "On" : "Off";
        return $"<color={Colors.Color32ToHex(color)}>{@bool}</color>";
    }

    internal override void SyncValue(object value, bool popNoti = true)
    {
        if (value is not bool @bool) return;
        SyncedValue = @bool;
        if (popNoti) PopNotification();
        UpdateVisuals();
        SyncAUOption();
    }

    internal override bool GetBool() => GetValue();
    internal override bool Is(bool @bool) => @bool == GetBool();
}

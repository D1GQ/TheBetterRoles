using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

/// <summary>
/// Int option.
/// Type: Int
/// </summary>
internal class OptionIntItem : OptionItem<int>
{
    internal override bool ShowChildren => base.ShowChildren && MainValue > 0;
    internal Int32OptionNames? VanillaOption { get; set; }
    protected IntRange Range { get; set; }
    protected int Increment { get; set; }
    protected bool CanBeInfinite { get; set; }
    protected (string prefix, string postfix) Fixs { get; set; }

    /// <summary>
    /// Creates a new integer item for the options menu. If an item with the specified ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <param name="id">The unique identifier for the integer item.</param>
    /// <param name="tab">The tab to which the integer item belongs.</param>
    /// <param name="tranStr">The translation string for the integer item label.</param>
    /// <param name="Min_Max_Increment">A tuple containing the minimum value, maximum value, and increment value for the integer item's range.</param>
    /// <param name="defaultValue">The default value for the integer item.</param>
    /// <param name="Prefix_Postfix">A tuple containing the prefix and postfix to be displayed alongside the integer value.</param>
    /// <param name="parent">An optional parent option item that this integer item belongs to.</param>
    /// <param name="canBeInfinite">A flag indicating whether the integer value can be infinite.</param>
    /// <param name="vanillaOption">An optional vanilla option name, if any, for this integer item.</param>
    /// <returns>The created or reused <see cref="OptionIntItem"/> instance.</returns>
    internal static OptionIntItem Create(int id, OptionTab tab, string tranStr, (int minValue, int maxValue, int incrementValue) Min_Max_Increment, int defaultValue, (string prefix, string postfix) Prefix_Postfix = new(), OptionItem parent = null, bool canBeInfinite = false, Int32OptionNames? vanillaOption = null)
    {
        if (GetOptionById(id) is OptionIntItem intItem)
        {
            intItem.CreateBehavior();
            return intItem;
        }

        OptionIntItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.Increment = Min_Max_Increment.incrementValue;
        Item.CanBeInfinite = canBeInfinite;
        Item.Range = new IntRange(Min_Max_Increment.minValue, Min_Max_Increment.maxValue);
        Item.DefaultValue = defaultValue;
        Item.Fixs = Prefix_Postfix;
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
        var numberOption = UnityEngine.Object.Instantiate(Tab.AUTab.numberOptionOrigin, Tab.AUTab.settingsContainer);
        Option = numberOption;
        Obj = Option.gameObject;
        Option.enabled = false;
        Tab.Children.Add(this);
        TitleTMP = numberOption.TitleText;
        ValueTMP = numberOption.ValueText;
        SetupText(numberOption.TitleText);
        SetupOptionBehavior();
        SetOptionVisuals();
    }

    protected override void SetupOptionBehavior()
    {
        if (Option is NumberOption numberOption)
        {
            SetupAUOption(Option);
            numberOption.DestroyTextTranslators();
            numberOption.TitleText.text = Name;
            numberOption.PlusBtn.OnClick = new();
            numberOption.PlusBtn.OnClick.AddListener((Action)(() => Increase()));
            numberOption.MinusBtn.OnClick = new();
            numberOption.MinusBtn.OnClick.AddListener((Action)(() => Decrease()));
        }
    }

    private void Increase()
    {
        int plus = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            plus = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            plus = 10;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl))
            plus = 25;
        var value = Value;
        value += Increment * plus;
        SetValue(value);
    }

    private void Decrease()
    {
        int plus = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            plus = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            plus = 10;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl))
            plus = 25;
        var value = Value;
        value -= Increment * plus;
        SetValue(value);
    }

    internal override void SyncAUOption()
    {
        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetInt((Int32OptionNames)VanillaOption, Value);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetInt((Int32OptionNames)VanillaOption, Value);
        }
    }

    internal override void SetValue(int newValue)
    {
        newValue = Math.Clamp(newValue, Range.min, Range.max);
        base.SetValue(newValue);
    }

    internal override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (Option is NumberOption numberOption)
        {
            numberOption.PlusBtn.SetInteractable(false);
            numberOption.MinusBtn.SetInteractable(false);

            if (GameState.IsHost || !GameState.IsInGame)
            {
                if (Value < Range.max)
                {
                    numberOption.PlusBtn.SetInteractable(true);
                }
                if (Value > Range.min)
                {
                    numberOption.MinusBtn.SetInteractable(true);
                }
            }

            numberOption.ValueText.text = ValueAsString().Replace(InfiniteIcon, InfiniteIcon.Size(200f));
        }

        if (updateTabVisuals)
        {
            Tab.UpdateVisuals();
        }
    }

    internal override void SyncValue(object value, bool popNoti = true)
    {
        if (value is not int @int) return;
        SyncedValue = @int;
        if (popNoti) PopNotification();
        UpdateVisuals();
        SyncAUOption();
    }

    internal override string ValueAsString()
    {
        if (CanBeInfinite)
        {
            if (MainValue <= 0)
            {
                return InfiniteIcon;
            }
        }

        return $"{Fixs.prefix}{MainValue}{Fixs.postfix}";
    }

    internal override int GetInt() => GetValue();
    internal override float GetFloat() => GetValue();
    internal override bool Is(int @int) => @int == GetInt();
    internal override bool Is(float @float) => @float == GetFloat();
}

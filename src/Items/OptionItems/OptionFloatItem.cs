using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionFloatItem : OptionItem<float>
{
    internal override bool ShowChildren => base.ShowChildren && MainValue > 0f;
    internal FloatOptionNames? VanillaOption { get; set; }
    protected FloatRange Range { get; set; }
    protected float Increment { get; set; }
    protected bool CanBeInfinite { get; set; }
    protected (string prefix, string postfix) Fixs { get; set; }

    /// <summary>
    /// Creates a new float item for the options menu. If an item with the specified ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <param name="id">The unique identifier for the float item.</param>
    /// <param name="tab">The tab to which the float item belongs.</param>
    /// <param name="tranStr">The translation string for the float item label.</param>
    /// <param name="Min_Max_Increment">A tuple containing the minimum value, maximum value, and increment value for the float item's range.</param>
    /// <param name="defaultValue">The default value for the float item.</param>
    /// <param name="Prefix_Postfix">A tuple containing the prefix and postfix to be displayed alongside the float value.</param>
    /// <param name="parent">An optional parent option item that this float item belongs to.</param>
    /// <param name="canBeInfinite">A flag indicating whether the float value can be infinite.</param>
    /// <param name="vanillaOption">An optional vanilla option name, if any, for this float item.</param>
    /// <returns>The created or reused <see cref="OptionFloatItem"/> instance.</returns>
    internal static OptionFloatItem Create(int id, OptionTab tab, string tranStr, (float minValue, float maxValue, float incrementValue) Min_Max_Increment, float defaultValue, (string prefix, string postfix) Prefix_Postfix = new(), OptionItem parent = null, bool canBeInfinite = false, FloatOptionNames? vanillaOption = null)
    {
        if (GetOptionById(id) is OptionFloatItem floatItem)
        {
            floatItem.CreateBehavior();
            return floatItem;
        }

        OptionFloatItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.Increment = Min_Max_Increment.incrementValue;
        Item.CanBeInfinite = canBeInfinite;
        Item.Range = new FloatRange(Min_Max_Increment.minValue, Min_Max_Increment.maxValue);
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

    protected void Increase()
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

    protected void Decrease()
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
            Main.CurrentOptions?.SetFloat((FloatOptionNames)VanillaOption, Value);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetFloat((FloatOptionNames)VanillaOption, Value);
        }
    }

    internal override void SetValue(float newValue)
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
        if (value is not float @float) return;
        SyncedValue = @float;
        if (popNoti) PopNotification();
        UpdateVisuals();
        SyncAUOption();
    }

    internal override string ValueAsString()
    {
        if (CanBeInfinite)
        {
            if (MainValue <= 0f)
            {
                return InfiniteIcon;
            }
        }

        return $"{Fixs.prefix}{MainValue}{Fixs.postfix}";
    }

    internal override float GetFloat() => GetValue();
    internal override bool Is(float @float) => @float == GetFloat();
    internal override bool Is(int @int) => @int == GetFloat();
}

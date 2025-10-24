using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Items.OptionItems;

/// <summary>
/// String option.
/// Type: Int
/// </summary>
internal class OptionStringItem : OptionItem<int>
{
    internal Int32OptionNames? VanillaOption { get; set; }
    protected IntRange Range { get; set; }
    protected string[] TranslatorStrings { get; set; }
    protected bool CanBeRandom { get; set; }

    /// <summary>
    /// Creates a new string item for the options menu. If an item with the specified ID already exists, 
    /// it reuses the existing item and sets up its behavior. The method allows specifying translation strings 
    /// and whether the option can be randomized.
    /// </summary>
    /// <param name="id">The unique identifier for the string item.</param>
    /// <param name="tab">The tab to which the string item belongs.</param>
    /// <param name="tranStr">The translation string for the string item label.</param>
    /// <param name="tranStrings">An array of translation strings representing the available options for the string item.</param>
    /// <param name="defaultValue">The default value (index) for the string item. If less than 0, the option can be randomized.</param>
    /// <param name="parent">An optional parent option item that this string item belongs to.</param>
    /// <param name="canBeRandom">A flag indicating whether the string item can have a random value.</param>
    /// <param name="vanillaOption">An optional vanilla option name, if any, for this string item.</param>
    /// <returns>The created or reused <see cref="OptionStringItem"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tranStrings"/> has fewer than 2 strings.</exception>
    internal static OptionStringItem Create(int id, OptionTab tab, string tranStr, string[] tranStrings, int defaultValue, OptionItem parent = null, bool canBeRandom = false, Int32OptionNames? vanillaOption = null)
    {
        if (tranStrings.Length < 2)
        {
            throw new ArgumentException("tranStrings must have more then 1 string!");
        }

        if (GetOptionById(id) is OptionStringItem stringItem)
        {
            stringItem.CreateBehavior();
            return stringItem;
        }

        if (defaultValue < 0)
        {
            canBeRandom = true;
        }

        OptionStringItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.TranslatorStrings = tranStrings;
        Item.Range = new IntRange(0, tranStrings.Length - 1);
        Item.DefaultValue = !canBeRandom ? defaultValue : defaultValue + 1;
        Item.CanBeRandom = canBeRandom;
        Item.VanillaOption = vanillaOption;
        if (canBeRandom)
        {
            var list = Item.TranslatorStrings.ToList();
            list.Insert(0, "Option.RandomWithColor");
            Item.TranslatorStrings = [.. list];
            Item.Range = new IntRange(0, list.Count - 1);
        }

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

    protected virtual void Increase()
    {
        SetValue(Value + 1);
    }

    protected virtual private void Decrease()
    {
        SetValue(Value - 1);
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

            numberOption.ValueText.text = $"{ValueAsString()}";
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

    internal override string ValueAsString() => Translator.GetString(TranslatorStrings[Value], showInvalid: false);
    internal override int GetStringValue()
    {
        var value = GetValue();
        if (!CanBeRandom)
        {
            return value;
        }
        else
        {
            if (value == 0)
            {
                return TranslatorStrings.Skip(1).RandomIndex().index;
            }
            else
            {
                return value - 1;
            }
        }
    }

    internal override bool Is(string @string) => TranslatorStrings[MainValue] == @string || ValueAsString() == @string;
    internal override bool Is(int @int) => !CanBeRandom ? MainValue == @int : MainValue == (@int - 1);
}

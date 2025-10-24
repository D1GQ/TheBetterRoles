using Reactor.Networking.Rpc;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;

namespace TheBetterRoles.Items.OptionItems;

/// <summary>
/// Preset option.
/// Type: Int
/// </summary>
internal class OptionPresetItem : OptionStringItem
{
    internal override bool CanLoad => false;

    /// <summary>
    /// Creates a new preset item for the options menu. If an item with the preset ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <returns>The created or reused <see cref="OptionPresetItem"/> instance.</returns>
    internal static OptionPresetItem Create()
    {
        int id = int.MaxValue;

        if (GetOptionById(id) is OptionPresetItem stringItem)
        {
            stringItem.CreateBehavior();
            return stringItem;
        }

        OptionPresetItem Item = new();
        AllTBROptions.Add(Item);
        Item._id = id;
        Item.Tab = TBRTabs.SystemSettings;
        Item.Translation = "Setting.Presets";
        Item.TranslatorStrings = Enumerable.Repeat(string.Empty, 10).ToArray();
        Item.Range = new IntRange(0, 10);
        Item.DefaultValue = 0;
        Item.Value = Main.Preset.Value;

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

    internal override void OnValueChange(int oldValue, int newValue)
    {
        Main.Preset.Value = newValue;
        TBRDataManager.GameSettingsFile.Load();
        foreach (var opt in AllTBROptions)
        {
            opt.TryLoad(true);
        }
        TBRTabs.SystemSettings.UpdateVisuals();
        if (GameState.IsHost)
        {
            Rpc<RpcSyncAllSettings>.Instance.Send(new());
        }
    }

    internal override string ValueAsString()
    {
        if (!GameState.IsHost && GameState.IsInGame)
        {
            return "Host Preset";
        }

        return $"Preset {Value}";
    }

    internal override void SyncValue(object value, bool popNoti = true)
    {
        if (value is not int @int) return;
        SyncedValue = @int;
        if (popNoti) PopNotification($"Preset {MainValue}");
        UpdateVisuals();

        if (GameSettingMenu.Instance)
        {
            GameSettingMenu.Instance.ChangeTab(GameSettingMenuPatch.ActiveTab, false);
        }
    }
}

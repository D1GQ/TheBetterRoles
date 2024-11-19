using Reactor.Networking.Rpc;
using System.Text.Json;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class TBROptionPresetItem : TBROptionItem
{
    private NumberOption? ThisOption;
    public int CurrentValue;
    public IntRange intRange = new(0, 180);
    public int Increment = 1;

    public override bool ShowChildrenCondition() => CurrentValue > intRange.min;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public TBROptionPresetItem Create(TBROptionTab gameOptionsMenu, int DefaultValue)
    {
        intRange = new(1, 10);
        Increment = 1;
        if (DefaultValue < intRange.min) DefaultValue = intRange.min;
        if (DefaultValue > intRange.max) DefaultValue = intRange.max;
        CurrentValue = DefaultValue;
        ShowCondition = null;

        if (gameOptionsMenu?.Tab == null || GameSettingMenuPatch.Preload)
        {
            return this;
        }

        NumberOption optionBehaviour = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.numberOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);

        optionBehaviour.PlusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.MinusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.PlusBtn.OnClick.AddListener(new Action(() => Increase()));
        optionBehaviour.MinusBtn.OnClick.AddListener(new Action(() => Decrease()));

        optionBehaviour.LabelBackground.transform.localScale = new Vector3(1.6f, 0.78f);
        optionBehaviour.LabelBackground.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.enableAutoSizing = false;
        optionBehaviour.TitleText.transform.SetLocalX(-1.5f);
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Right;
        optionBehaviour.TitleText.enableWordWrapping = false;
        optionBehaviour.TitleText.fontSize = 2.5f;

        // Set data
        Tab = gameOptionsMenu;
        Name = Translator.GetString("BetterSetting.Presets");
        TitleText = optionBehaviour.TitleText;
        Option = optionBehaviour;
        ThisOption = optionBehaviour;

        Load(DefaultValue);
        AdjustButtonsActiveState();

        BetterOptionItems.Add(this);
        obj = optionBehaviour.gameObject;

        return this;
    }

    private void AdjustPreset()
    {
        TBRDataManager.TempSettings.Clear();
        TBRDataManager._settingsFileCache.Clear();
        if (!File.Exists(TBRDataManager.SettingsFile))
        {
            var initialData = new Dictionary<string, string>();
            string json = JsonSerializer.Serialize(initialData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TBRDataManager.SettingsFile, json);
        }

        GameSettingMenu.Instance.Close();
        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        localPlayer.NetTransform.Halt();
        GameObject gameObject = UnityEngine.Object.Instantiate(GameStartManager.Instance.PlayerOptionsMenu);
        gameObject.transform.SetParent(Camera.main.transform, false);
        gameObject.transform.localPosition = GameStartManager.Instance.GameOptionsPosition;
        DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(null, gameObject.gameObject, null);
        GameStartManager.Instance.RulesViewPanel.SetActive(false);
        GameStartManager.Instance.SelectViewButton(false);
        GameStartManager.Instance.LobbyInfoPane.DeactivatePane();
        _ = new LateTask(() =>
        {
            GameSettingMenu.Instance.ChangeTab(BetterTabs.SystemSettings.Id, false);
            Rpc<RpcSyncAllSettings>.Instance.Send(new());
        }, 0.25f, shouldLog: false);
    }

    private void AdjustButtonsActiveState()
    {
        if (ThisOption == null) return;

        ThisOption.ValueText.text = GameState.IsHost || !GameState.IsInGame || GameState.IsFreePlay ? Translator.GetString("BetterSetting.Preset") + " " + CurrentValue.ToString() : Translator.GetString(StringNames.HostHeader);

        if (!GameState.IsHost && GameState.IsInGame && !GameState.IsFreePlay)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(false);
            return;
        }

        ThisOption.PlusBtn.SetInteractable(true);
        ThisOption.MinusBtn.SetInteractable(true);

        if (CurrentValue >= intRange.max) ThisOption.PlusBtn.SetInteractable(false);
        if (CurrentValue <= intRange.min) ThisOption.MinusBtn.SetInteractable(false);

        Main.Preset.Value = CurrentValue;
    }

    public void Increase()
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (CurrentValue + Increment * times <= intRange.max)
        {
            CurrentValue += Increment * times;
        }
        else
        {
            CurrentValue = intRange.max;
        }

        AdjustButtonsActiveState();
        AdjustPreset();
    }

    public void Decrease()
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (CurrentValue - Increment * times >= intRange.min)
        {
            CurrentValue -= Increment * times;
        }
        else
        {
            CurrentValue = intRange.min;
        }

        AdjustButtonsActiveState();
        AdjustPreset();
    }

    public void Load(int DefaultValue)
    {
        if (Main.Preset != null)
        {
            CurrentValue = Main.Preset.Value;
        }
        else
        {
            CurrentValue = DefaultValue;
        }
    }

    public override float GetFloat()
    {
        if (TBRDataManager.CanLoadSetting(Id))
        {
            return TBRDataManager.LoadIntSetting(Id);
        }
        else
        {
            return CurrentValue;
        }
    }

    public override int GetInt()
    {
        if (TBRDataManager.CanLoadSetting(Id))
        {
            return TBRDataManager.LoadIntSetting(Id);
        }
        else
        {
            return CurrentValue;
        }
    }

    public override void SetData(OptionBehaviour optionBehaviour)
    {
        optionBehaviour.data = new BaseGameSetting
        {
            Title = StringNames.None,
            Type = OptionTypes.Int,
        };
    }
}

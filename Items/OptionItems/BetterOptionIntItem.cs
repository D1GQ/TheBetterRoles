using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionIntItem : BetterOptionItem
{
    public Int32OptionNames? VanillaOption;

    private NumberOption? ThisOption;
    public int CurrentValue;
    public int defaultValue;
    public IntRange intRange = new(0, 180);
    public int Increment = 1;
    private string? PostFix;
    private string? PreFix;

    public override bool ShowChildrenCondition() => CurrentValue > intRange.min;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public BetterOptionIntItem Create(int id, BetterOptionTab gameOptionsMenu, string name, int[] values, int DefaultValue, string preFix = "", string postFix = "", BetterOptionItem? Parent = null, Func<bool>? selfShowCondition = null, Int32OptionNames? vanillaOption = null)
    {
        Id = id >= 0 ? id : GetGeneratedId();
        intRange = new(values[0], values[1]);
        Increment = values[2];
        if (DefaultValue < intRange.min) DefaultValue = intRange.min;
        if (DefaultValue > intRange.max) DefaultValue = intRange.max;
        CurrentValue = DefaultValue;
        defaultValue = DefaultValue;
        ShowCondition = selfShowCondition;
        VanillaOption = vanillaOption;

        if (gameOptionsMenu?.Tab == null || !GameState.IsLobby)
        {
            Load(DefaultValue);
            BetterOptionItems.Add(this);
            return this;
        }

        if (GameSettingMenuPatch.Preload)
        {
            Load(DefaultValue);
            if (BetterOptionItems.Any(op => op.Id == Id))
            {
                return (BetterOptionIntItem)BetterOptionItems.First(op => op.Id == Id);
            }
            else
            {
                BetterOptionItems.Add(this);
                return this;
            }
        }

        if (values.Length is < 3 or > 3) return null;

        NumberOption optionBehaviour = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.numberOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(Id, option));

        // Fix Game Crash
        foreach (RulesCategory rulesCategory in GameManager.Instance.GameSettingsList.AllCategories)
        {
            optionBehaviour.data = rulesCategory.AllGameSettings.ToArray().FirstOrDefault(item => item.Type == OptionTypes.Number);
        }

        optionBehaviour.PlusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.MinusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.PlusBtn.OnClick.AddListener(new Action(() => Increase()));
        optionBehaviour.MinusBtn.OnClick.AddListener(new Action(() => Decrease()));
        optionBehaviour.PlusBtn.OnClick.AddListener(new Action(() => ValueChanged(Id, optionBehaviour)));
        optionBehaviour.MinusBtn.OnClick.AddListener(new Action(() => ValueChanged(Id, optionBehaviour)));

        optionBehaviour.LabelBackground.transform.localScale = new Vector3(1.6f, 0.78f);
        optionBehaviour.LabelBackground.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.enableAutoSizing = false;
        optionBehaviour.TitleText.transform.SetLocalX(-1.5f);
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Right;
        optionBehaviour.TitleText.enableWordWrapping = false;
        optionBehaviour.TitleText.fontSize = 2.5f;

        // Set data
        Tab = gameOptionsMenu;
        Name = name;
        TitleText = optionBehaviour.TitleText;
        Option = optionBehaviour;
        PostFix = postFix;
        PreFix = preFix;
        ThisOption = optionBehaviour;

        Load(DefaultValue);
        AdjustButtonsActiveState();

        BetterOptionItems.Add(this);
        obj = optionBehaviour.gameObject;

        if (Parent != null)
        {
            int Index = 1;
            var tempParent = Parent;

            while (tempParent.ThisParent != null)
            {
                tempParent = tempParent.ThisParent;
                Index++;
            }

            optionBehaviour.LabelBackground.GetComponent<SpriteRenderer>().color -= new Color(0.25f, 0.25f, 0.25f, 0f) * Index;
            optionBehaviour.LabelBackground.transform.localScale -= new Vector3(0.04f, 0f, 0f) * Index;
            optionBehaviour.LabelBackground.transform.position += new Vector3(0.04f, 0f, 0f) * Index;
            optionBehaviour.LabelBackground.transform.SetLocalZ(1f);
            ThisParent = Parent;
            IsChild = true;
            Parent.ChildrenList.Add(this);
        }

        return this;
    }

    private void AdjustButtonsActiveState()
    {
        if (ThisOption == null) return;

        ThisOption.ValueText.text = PreFix + CurrentValue.ToString() + PostFix;

        if (!GameState.IsHost)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(false);
            return;
        }

        ThisOption.PlusBtn.SetInteractable(true);
        ThisOption.MinusBtn.SetInteractable(true);

        if (CurrentValue >= intRange.max) ThisOption.PlusBtn.SetInteractable(false);
        if (CurrentValue <= intRange.min) ThisOption.MinusBtn.SetInteractable(false);

        BetterDataManager.SaveSetting(Id, CurrentValue.ToString());
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
        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetInt((Int32OptionNames)VanillaOption, CurrentValue);
            Main.SetVanillaSettings();
        }
        RPC.SyncOption(Id, CurrentValue.ToString(), FormatValueAsText());
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
        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetInt((Int32OptionNames)VanillaOption, CurrentValue);
            Main.SetVanillaSettings();
        }
        RPC.SyncOption(Id, CurrentValue.ToString(), FormatValueAsText());
    }

    public void Load(int DefaultValue)
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            var Int = BetterDataManager.LoadIntSetting(Id, DefaultValue);

            if (Int > intRange.max || Int < intRange.min)
            {
                Int = DefaultValue;
                BetterDataManager.SaveSetting(Id, DefaultValue.ToString());
            }

            CurrentValue = Int;
        }
        else
        {
            BetterDataManager.SaveSetting(Id, DefaultValue.ToString());
        }

        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetInt((Int32OptionNames)VanillaOption, CurrentValue);
            Main.SetVanillaSettings();
        }
    }

    public override float GetFloat()
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            return BetterDataManager.LoadIntSetting(Id);
        }
        else
        {
            return CurrentValue;
        }
    }

    public override int GetInt()
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            return BetterDataManager.LoadIntSetting(Id);
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

    public override void ValueChanged(int id, OptionBehaviour optionBehaviour)
    {
        if (IsParent || IsChild)
        {
            bool Bool = ShowChildrenCondition();
            foreach (var item in ChildrenList)
            {
                item.obj.SetActive(Bool && item.SelfShowCondition());
            }
            UpdatePositions();
        }
    }

    public override void SyncValue(string value)
    {
        if (!int.TryParse(value, out int @int)) return;

        CurrentValue = @int;

        if (ThisOption)
        {
            ThisOption.ValueText.text = PreFix + CurrentValue.ToString() + PostFix;

            if (IsParent || IsChild)
            {
                bool Bool = ShowChildrenCondition();
                foreach (var item in ChildrenList)
                {
                    item.obj.SetActive(Bool && item.SelfShowCondition());
                }
                UpdatePositions();
            }
        }

        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetInt((Int32OptionNames)VanillaOption, CurrentValue);
            Main.SetVanillaSettings();
        }
    }

    public override string FormatValueAsText()
    {
        return $"{PreFix}{CurrentValue}{PostFix}";
    }
}

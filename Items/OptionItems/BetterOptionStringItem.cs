using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class BetterOptionStringItem : BetterOptionItem
{
    private StringOption? ThisOption;
    public string[] Values = [];
    public int CurrentValue;
    public int defaultValue;

    public override bool ShowChildrenCondition() => CurrentValue > 0;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public BetterOptionItem Create(int id, BetterOptionTab gameOptionsMenu, string name, string[] strings, int DefaultValue = 0, BetterOptionItem? Parent = null, Func<bool>? selfShowCondition = null)
    {
        Id = id >= 0 ? id : GetGeneratedId();
        Values = strings;
        Tab = gameOptionsMenu;
        Name = name;
        if (DefaultValue < 0) DefaultValue = 0;
        if (DefaultValue > Values.Length) DefaultValue = Values.Length;
        CurrentValue = DefaultValue;
        defaultValue = DefaultValue;
        ShowCondition = selfShowCondition;

        if (gameOptionsMenu?.Tab == null || !GameStates.IsLobby)
        {
            Load(DefaultValue);
            BetterOptionItems.Add(this);
            return this;
        }

        if (GameSettingMenuPatch.Preload)
        {
            Load(DefaultValue);
            if (BetterOptionItems.Any(op => op.Id == id))
            {
                return BetterOptionItems.First(op => op.Id == id);
            }
            else
            {
                BetterOptionItems.Add(this);
                return this;
            }
        }

        StringOption optionBehaviour = UnityEngine.Object.Instantiate<StringOption>(gameOptionsMenu.Tab.stringOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(id, option));

        // Fix Game Crash
        foreach (RulesCategory rulesCategory in GameManager.Instance.GameSettingsList.AllCategories)
        {
            optionBehaviour.data = rulesCategory.AllGameSettings.ToArray().FirstOrDefault(item => item.Type == OptionTypes.String);
        }

        optionBehaviour.PlusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.MinusBtn.OnClick.RemoveAllListeners();
        optionBehaviour.PlusBtn.OnClick.AddListener(new Action(() => Increase()));
        optionBehaviour.MinusBtn.OnClick.AddListener(new Action(() => Decrease()));
        optionBehaviour.PlusBtn.OnClick.AddListener(new Action(() => ValueChanged(id, optionBehaviour)));
        optionBehaviour.MinusBtn.OnClick.AddListener(new Action(() => ValueChanged(id, optionBehaviour)));

        optionBehaviour.LabelBackground.transform.localScale = new Vector3(1.6f, 0.78f);
        optionBehaviour.LabelBackground.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.enableAutoSizing = false;
        optionBehaviour.TitleText.transform.SetLocalX(-1.5f);
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Right;
        optionBehaviour.TitleText.enableWordWrapping = false;
        optionBehaviour.TitleText.fontSize = 2.5f;

        // Set data
        TitleText = optionBehaviour.TitleText;
        Option = optionBehaviour;
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

        if (CurrentValue <= Values.Length - 1 && CurrentValue >= 0)
        {
            ThisOption.ValueText.text = Values[CurrentValue];
        }

        if (!GameStates.IsHost)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(false);
            return;
        }

        ThisOption.PlusBtn.SetInteractable(true);
        ThisOption.MinusBtn.SetInteractable(true);

        if (CurrentValue >= Values.Length - 1) ThisOption.PlusBtn.SetInteractable(false);
        if (CurrentValue <= 0) ThisOption.MinusBtn.SetInteractable(false);

        BetterDataManager.SaveSetting(Id, CurrentValue.ToString());
    }

    public void Increase()
    {
        if (CurrentValue < Values.Length)
        {
            CurrentValue++;
            AdjustButtonsActiveState();
            RPC.SyncOption(Id, CurrentValue.ToString(), Values[CurrentValue]);
        }
    }

    public void Decrease()
    {
        if (CurrentValue > 0)
        {
            CurrentValue--;
            AdjustButtonsActiveState();
            RPC.SyncOption(Id, CurrentValue.ToString(), Values[CurrentValue]);
        }
    }

    public void Load(int DefaultValue)
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            var Int = BetterDataManager.LoadIntSetting(Id, DefaultValue);

            if (Int > Values.Length - 1 || Int < 0)
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
    }

    public override int GetValue()
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
            Type = OptionTypes.String,
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
}

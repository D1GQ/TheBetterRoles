using UnityEngine;

namespace TheBetterRoles;

public class BetterOptionFloatItem : BetterOptionItem
{
    private NumberOption? ThisOption;
    public float CurrentValue;
    public FloatRange floatRange = new(0f, 180f);
    public float Increment = 2.5f;
    private string? PostFix;
    private string? PreFix;

    public override bool ShowChildrenCondition() => CurrentValue > floatRange.min;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public BetterOptionItem Create(int id, BetterOptionTab gameOptionsMenu, string name, float[] values, float DefaultValue, string preFix = "", string postFix = "", BetterOptionItem? Parent = null, Func<bool>? selfShowCondition = null)
    {
        Id = id >= 0 ? id : GetGeneratedId();
        floatRange = new(values[0], values[1]);
        Increment = values[2];
        if (DefaultValue < floatRange.min) DefaultValue = floatRange.min;
        if (DefaultValue > floatRange.max) DefaultValue = floatRange.max;
        CurrentValue = DefaultValue;
        ShowCondition = selfShowCondition;

        if (gameOptionsMenu?.Tab == null || !GameStates.IsLobby)
        {
            Load(DefaultValue);
            return this;
        }

        if (values.Length is < 3 or > 3) return null;

        NumberOption optionBehaviour = UnityEngine.Object.Instantiate<NumberOption>(gameOptionsMenu.Tab.numberOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(id, option));

        // Fix Game Crash
        foreach (RulesCategory rulesCategory in GameManager.Instance.GameSettingsList.AllCategories)
        {
            optionBehaviour.data = rulesCategory.AllGameSettings.ToArray().FirstOrDefault(item => item.Type == OptionTypes.Number);
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

        ThisOption.PlusBtn.SetInteractable(true);
        ThisOption.MinusBtn.SetInteractable(true);

        if (CurrentValue >= floatRange.max) ThisOption.PlusBtn.SetInteractable(false);
        if (CurrentValue <= floatRange.min) ThisOption.MinusBtn.SetInteractable(false);

        BetterDataManager.SaveSetting(Id, CurrentValue.ToString());
    }

    public void Increase()
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (CurrentValue + Increment * times <= floatRange.max)
        {
            CurrentValue += Increment * times;
        }
        else
        {
            CurrentValue = floatRange.max;
        }

        AdjustButtonsActiveState();
    }

    public void Decrease()
    {
        int times = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            times = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            times = 10;

        if (CurrentValue - Increment * times >= floatRange.min)
        {
            CurrentValue -= Increment * times;
        }
        else
        {
            CurrentValue = floatRange.min;
        }

        AdjustButtonsActiveState();
    }

    public void Load(float DefaultValue)
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            var Float = BetterDataManager.LoadFloatSetting(Id, DefaultValue);

            if (Float > floatRange.max || Float < floatRange.min)
            {
                Float = DefaultValue;
                BetterDataManager.SaveSetting(Id, DefaultValue.ToString());
            }

            CurrentValue = Float;
        }
        else
        {
            BetterDataManager.SaveSetting(Id, DefaultValue.ToString());
        }
    }

    public override float GetFloat()
    {
        if (BetterDataManager.CanLoadSetting(Id))
        {
            return BetterDataManager.LoadFloatSetting(Id);
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
            return (int)BetterDataManager.LoadFloatSetting(Id);
        }
        else
        {
            return (int)CurrentValue;
        }
    }

    public override void SetData(OptionBehaviour optionBehaviour)
    {
        optionBehaviour.data = new BaseGameSetting
        {
            Title = StringNames.None,
            Type = OptionTypes.Float,
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

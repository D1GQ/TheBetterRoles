using Reactor.Networking.Rpc;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionPercentItem : BetterOptionItem
{
    private NumberOption? ThisOption;
    public float CurrentValue;
    public float defaultValue;
    public FloatRange floatRange => new(0f, 100f);
    public float Increment = 5f;

    public override bool ShowChildrenCondition() => CurrentValue > floatRange.min;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public BetterOptionPercentItem Create(int id, BetterOptionTab gameOptionsMenu, string name, float DefaultValue, BetterOptionItem? Parent = null, Func<bool>? selfShowCondition = null)
    {
        Id = id >= 0 ? id : GetGeneratedId();
        Name = name;
        if (DefaultValue < floatRange.min) DefaultValue = floatRange.min;
        if (DefaultValue > floatRange.max) DefaultValue = floatRange.max;
        CurrentValue = DefaultValue;
        defaultValue = DefaultValue;
        ShowCondition = selfShowCondition;

        if (GameSettingMenuPatch.Preload || gameOptionsMenu?.Tab == null)
        {
            Load(DefaultValue);
            if (BetterOptionItems.Any(op => op.Id == Id))
            {
                return (BetterOptionPercentItem)BetterOptionItems.First(op => op.Id == Id);
            }
            else
            {
                BetterOptionItems.Add(this);
                if (Parent != null)
                {
                    int Index = 1;
                    var tempParent = Parent;

                    while (tempParent.ThisParent != null)
                    {
                        tempParent = tempParent.ThisParent;
                        Index++;
                    }
                    ThisParent = Parent;
                    IsChild = true;
                    Parent.ChildrenList.Add(this);
                }
                return this;
            }
        }

        NumberOption optionBehaviour = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.numberOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(Id, option));

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
        TitleText = optionBehaviour.TitleText;
        TitleText.outlineColor = Color.black;
        TitleText.outlineWidth = 0.2f;
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

        ThisOption.ValueText.text = $"<color={GetColor(CurrentValue)}>{CurrentValue}%</color>";

        if (!GameState.IsHost && GameState.IsInGame && !GameState.IsFreePlay)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(false);
            return;
        }

        ThisOption.PlusBtn.SetInteractable(true);
        ThisOption.MinusBtn.SetInteractable(true);

        if (CurrentValue >= floatRange.max) ThisOption.PlusBtn.SetInteractable(false);
        if (CurrentValue <= floatRange.min) ThisOption.MinusBtn.SetInteractable(false);

        BetterDataManager.SaveSetting(Id, CurrentValue.ToString());
    }

    public string GetColor(float num)
    {
        switch (num)
        {
            case float n when n <= 0f:
                return "#ff0600";
            case float n when n <= 25f:
                return "#ff9d00";
            case float n when n <= 50f:
                return "#fff900";
            case float n when n <= 75f:
                return "#80ff00";
            default:
                return "#80ff00";
        }
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
        Rpc<RpcSyncOption>.Instance.Send(new(Id, CurrentValue.ToString(), FormatValueAsText()));
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
        Rpc<RpcSyncOption>.Instance.Send(new(Id, CurrentValue.ToString(), FormatValueAsText()));
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
        OnValueChange.Invoke(this);

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
        if (!float.TryParse(value, out float @float)) return;

        CurrentValue = @float;

        if (ThisOption)
        {
            ThisOption.ValueText.text = $"<color={GetColor(CurrentValue)}>{CurrentValue}%</color>";

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

    public override string FormatValueAsText()
    {
        return $"<color={GetColor(CurrentValue)}>{CurrentValue}%</color>";
    }
}

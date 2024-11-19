using AmongUs.GameOptions;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class TBROptionCheckboxItem : TBROptionItem
{
    public BoolOptionNames? VanillaOption;

    private ToggleOption? ThisOption;
    public bool IsChecked;
    public bool defaultValue;

    public override bool ShowChildrenCondition() => IsChecked == true;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public TBROptionCheckboxItem Create(int id, TBROptionTab gameOptionsMenu, string name, bool DefaultValue = true, TBROptionItem? Parent = null, Func<bool>? selfShowCondition = null, BoolOptionNames? vanillaOption = null)
    {
        Id = id >= 0 ? id : GetGeneratedId();
        Tab = gameOptionsMenu;
        Name = name;
        IsChecked = DefaultValue;
        defaultValue = DefaultValue;
        ShowCondition = selfShowCondition;
        VanillaOption = vanillaOption;

        if (GameSettingMenuPatch.Preload || gameOptionsMenu?.Tab == null)
        {
            Load(DefaultValue);
            if (BetterOptionItems.Any(op => op.Id == Id))
            {
                return (TBROptionCheckboxItem)BetterOptionItems.First(op => op.Id == Id);
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

        ToggleOption optionBehaviour = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.checkboxOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(Id, option));

        optionBehaviour.LabelBackground.transform.localScale = new Vector3(1.6f, 0.78f);
        optionBehaviour.LabelBackground.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.enableAutoSizing = false;
        optionBehaviour.TitleText.transform.SetLocalX(-1.5f);
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Right;
        optionBehaviour.TitleText.enableWordWrapping = false;
        optionBehaviour.TitleText.fontSize = 2.5f;

        // Set data
        optionBehaviour.CheckMark.GetComponent<SpriteRenderer>().enabled = DefaultValue;
        TitleText = optionBehaviour.TitleText;
        Option = optionBehaviour;
        ThisOption = optionBehaviour;

        Load(DefaultValue);

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

        if (!GameState.IsHost && GameState.IsInGame && !GameState.IsFreePlay)
        {
            optionBehaviour.CheckMark.transform.parent.Find("ActiveSprite").gameObject.SetActive(false);
            optionBehaviour.CheckMark.transform.parent.Find("InactiveSprite").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
            optionBehaviour.CheckMark.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
            optionBehaviour.transform.Find("Toggle").GetComponent<PassiveButton>().enabled = false;
        }

        return this;
    }

    public void Load(bool DefaultValue)
    {
        if (TBRDataManager.CanLoadSetting(Id))
        {
            var Bool = TBRDataManager.LoadBoolSetting(Id, DefaultValue);
            if (ThisOption != null) ThisOption.CheckMark.GetComponent<SpriteRenderer>().enabled = Bool;
            IsChecked = Bool;
        }
        else
        {
            TBRDataManager.SaveSetting(Id, DefaultValue.ToString());
        }

        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
        }
    }

    public override bool GetBool()
    {
        if (TBRDataManager.CanLoadSetting(Id))
        {
            return TBRDataManager.LoadBoolSetting(Id);
        }
        else
        {
            return IsChecked;
        }
    }

    public override void SetData(OptionBehaviour optionBehaviour)
    {
        optionBehaviour.data = new BaseGameSetting
        {
            Title = StringNames.None,
            Type = OptionTypes.Checkbox,
        };
    }

    public override void ValueChanged(int id, OptionBehaviour optionBehaviour)
    {
        IsChecked = !IsChecked;

        OnValueChange.Invoke(this);

        TBRDataManager.SaveSetting(Id, IsChecked.ToString());

        if (VanillaOption != null)
        {
            Main.CurrentOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
        }

        Rpc<RpcSyncOption>.Instance.Send(new(Id, IsChecked.ToString(), FormatValueAsText()));

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
        if (!bool.TryParse(value, out bool @bool)) return;

        IsChecked = @bool;

        if (ThisOption)
        {
            var check = ThisOption.CheckMark.GetComponent<SpriteRenderer>();
            if (check != null)
            {
                check.enabled = IsChecked;
            }

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
            Main.CurrentOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
            GameManager.Instance?.LogicOptions?.currentGameOptions?.SetBool((BoolOptionNames)VanillaOption, IsChecked);
        }
    }

    public override string FormatValueAsText()
    {
        Color color = IsChecked ? Color.green : Color.red;
        string @bool = IsChecked ? "On" : "Off";
        return $"<color={Utils.Color32ToHex(color)}>{@bool}</color>";
    }
}

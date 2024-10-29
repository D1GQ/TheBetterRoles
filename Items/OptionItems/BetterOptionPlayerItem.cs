using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionPlayerItem : BetterOptionItem
{
    private NumberOption? ThisOption;
    public int CurrentIndex = -1;

    public override bool ShowChildrenCondition() => CurrentIndex > -1;
    public override bool SelfShowCondition() => ShowCondition != null ? ShowCondition() : base.SelfShowCondition();
    public Func<bool>? ShowCondition = null;

    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu, string name, BetterOptionItem? Parent = null, Func<bool>? selfShowCondition = null)
    {
        Id = TempPlayerOptionDataNum + 1;
        Tab = gameOptionsMenu;
        Name = name;
        ShowCondition = selfShowCondition;

        if (gameOptionsMenu?.Tab == null || GameSettingMenuPatch.Preload)
        {
            return this;
        }

        NumberOption optionBehaviour = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.numberOptionOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);
        optionBehaviour.OnValueChanged = new Action<OptionBehaviour>((option) => ValueChanged(0, option));

        // Fix Game Crash
        foreach (RulesCategory rulesCategory in GameManager.Instance.GameSettingsList.AllCategories)
        {
            optionBehaviour.data = rulesCategory.AllGameSettings.ToArray().FirstOrDefault(item => item.Type == OptionTypes.Player);
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
        TitleText = optionBehaviour.TitleText;
        Option = optionBehaviour;
        ThisOption = optionBehaviour;

        Load();
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

        TempPlayerOptionDataNum++;

        return this;
    }

    private void AdjustButtonsActiveState()
    {
        if (ThisOption == null) return;

        if (CurrentIndex >= 0)
        {
            ThisOption.ValueText.text = Utils.PlayerFromPlayerId(CurrentIndex).GetPlayerNameAndColor();
        }
        else
        {
            ThisOption.ValueText.text = "<color=#ababab>Random</color>";
        }

        if (!GameState.IsHost)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(false);
            return;
        }

        if (CurrentIndex >= Main.AllPlayerControls.Length - 1)
        {
            ThisOption.PlusBtn.SetInteractable(false);
            ThisOption.MinusBtn.SetInteractable(true);
        }
        else if (CurrentIndex <= -1)
        {
            ThisOption.PlusBtn.SetInteractable(true);
            ThisOption.MinusBtn.SetInteractable(false);
        }
        else
        {
            ThisOption.PlusBtn.SetInteractable(true);
            ThisOption.MinusBtn.SetInteractable(true);
        }

        TempPlayerOptionData[Id] = CurrentIndex;
    }

    public void Increase()
    {
        if (CurrentIndex < Main.AllPlayerControls.Length)
        {
            CurrentIndex++;
            AdjustButtonsActiveState();

            string text;
            if (CurrentIndex >= 0)
            {
                text = Utils.PlayerFromPlayerId(CurrentIndex).GetPlayerNameAndColor();
            }
            else
            {
                text = "<color=#ababab>Random</color>";
            }
            RPC.SyncOption(Id, CurrentIndex.ToString(), text);
        }
    }

    public void Decrease()
    {
        if (CurrentIndex > -1)
        {
            CurrentIndex--;
            AdjustButtonsActiveState();

            string text;
            if (CurrentIndex >= 0)
            {
                text = Utils.PlayerFromPlayerId(CurrentIndex).GetPlayerNameAndColor();
            }
            else
            {
                text = "<color=#ababab>Random</color>";
            }
            RPC.SyncOption(Id, CurrentIndex.ToString(), text);
        }
    }

    public int Load()
    {
        if (TempPlayerOptionData.ContainsKey(Id))
        {
            var saveindex = TempPlayerOptionData[Id];

            if (saveindex != -1 && Utils.PlayerFromPlayerId(saveindex) == null)
                saveindex = Main.AllPlayerControls.Length - 1;

            CurrentIndex = saveindex;
        }
        else
        {
            TempPlayerOptionData[Id] = -1;
            CurrentIndex = TempPlayerOptionData[Id];
        }

        AdjustButtonsActiveState();

        return CurrentIndex;
    }

    public override int GetValue()
    {
        if (SelfShowCondition())
        {
            return Load();
        }
        else
        {
            return -1;
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

    public override void SyncValue()
    {
        if (ThisOption)
        {
            AdjustButtonsActiveState();

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
}

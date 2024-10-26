using UnityEngine;

namespace TheBetterRoles;

public class BetterOptionTitleItem : BetterOptionItem
{
    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu, string name)
    {
        if (gameOptionsMenu.Tab == null)
        {
            return this;
        }

        ToggleOption optionBehaviour = UnityEngine.Object.Instantiate<ToggleOption>(gameOptionsMenu.Tab.checkboxOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetUp(optionBehaviour);

        optionBehaviour.CheckMark.transform.parent.gameObject.DestroyObj();
        optionBehaviour.GetComponent<ToggleOption>().DestroyMono();
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Center;
        optionBehaviour.TitleText.outlineColor = Color.black;
        optionBehaviour.TitleText.outlineWidth = 0.2f;
        optionBehaviour.TitleText.transform.localPosition += new Vector3(0f, 0.05f, 0f);
        optionBehaviour.LabelBackground.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        optionBehaviour.LabelBackground.transform.SetLocalZ(1f);

        optionBehaviour.LabelBackground.transform.localScale = new Vector3(1.6f, 1f);
        optionBehaviour.LabelBackground.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.enableAutoSizing = false;
        optionBehaviour.TitleText.transform.SetLocalX(-2.4f);
        optionBehaviour.TitleText.alignment = TMPro.TextAlignmentOptions.Center;
        optionBehaviour.TitleText.enableWordWrapping = false;
        optionBehaviour.TitleText.fontSize = 2.5f;

        // Set data
        Tab = gameOptionsMenu;
        Name = $"<b><size=150%>{name}</size></b>";
        Option = optionBehaviour;
        TitleText = optionBehaviour.TitleText;

        BetterOptionItems.Add(this);
        obj = optionBehaviour.gameObject;

        return this;
    }

    public override void SetData(OptionBehaviour optionBehaviour)
    {
        optionBehaviour.data = new BaseGameSetting
        {
            Title = StringNames.None,
            Type = OptionTypes.Checkbox,
        };
    }
}

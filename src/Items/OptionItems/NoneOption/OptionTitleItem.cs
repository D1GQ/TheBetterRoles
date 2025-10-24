using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionTitleItem : OptionItem
{
    private ToggleOption optionBehaviour;
    internal override bool CanLoad => false;
    internal (float top, float bottom) Distance { get; set; }

    /// <summary>
    /// Creates a new title item for the options menu. The title is placed within the specified tab with configurable top and bottom distances.
    /// </summary>
    /// <param name="tab">The tab where the title item will be placed.</param>
    /// <param name="tranStr">The translation string for the title item.</param>
    /// <param name="topDistance">The distance from the top of the tab to the title. Default is 0.15f.</param>
    /// <param name="bottomDistance">The distance from the bottom of the tab to the title. Default is 0.50f.</param>
    /// <returns>The created <see cref="OptionTitleItem"/> instance.</returns>
    internal static OptionTitleItem Create(OptionTab tab, string tranStr, float topDistance = 0.15f, float bottomDistance = 0.50f)
    {
        var Item = new OptionTitleItem
        {
            Translation = tranStr,
            Tab = tab,
            Distance = (topDistance, bottomDistance)
        };
        Item.CreateBehavior();

        return Item;
    }

    protected void CreateBehavior()
    {
        if (!GameSettingMenu.Instance) return;
        AllTBROptionsTemp.Add(this);
        optionBehaviour = UnityEngine.Object.Instantiate(Tab.AUTab.checkboxOrigin, Tab.AUTab.settingsContainer);
        Obj = optionBehaviour.gameObject;
        optionBehaviour.transform.localPosition = new Vector3(0.952f, 2f, -2f);
        SetupAUOption(optionBehaviour);

        optionBehaviour.CheckMark.transform.parent.gameObject.DestroyObj();
        optionBehaviour.GetComponent<ToggleOption>().DestroyMono();
        optionBehaviour.TitleText.DestroyTextTranslators();
        optionBehaviour.TitleText.text = Name;
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
        optionBehaviour.TitleText.fontSize = 3.5f;
        Tab.Children.Add(this);
    }

    internal override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (optionBehaviour != null)
        {
            optionBehaviour.TitleText.text = Name;
        }
    }

    internal override string ValueAsString()
    {
        throw new NotImplementedException();
    }

    internal override void SyncValue(object value, bool popNoti = true)
    {
        throw new NotImplementedException();
    }
}

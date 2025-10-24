using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionHeaderItem : OptionItem
{
    private CategoryHeaderMasked categoryHeaderMasked;
    internal override bool CanLoad => false;
    internal override bool IsOption => false;
    internal (float top, float bottom) Distance { get; set; }

    /// <summary>
    /// Creates a new header item for the options menu. The header is placed within the specified tab with configurable top and bottom distances.
    /// </summary>
    /// <param name="tab">The tab where the header item will be placed.</param>
    /// <param name="tranStr">The translation string for the header item.</param>
    /// <param name="topDistance">The distance from the top of the tab to the header. Default is 0.35f.</param>
    /// <param name="bottomDistance">The distance from the bottom of the tab to the header. Default is 0.80f.</param>
    /// <returns>The created <see cref="OptionHeaderItem"/> instance.</returns>
    internal static OptionHeaderItem Create(OptionTab tab, string tranStr, float topDistance = 0.35f, float bottomDistance = 0.80f)
    {
        var Item = new OptionHeaderItem
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
        categoryHeaderMasked = UnityEngine.Object.Instantiate(Tab.AUTab.categoryHeaderOrigin, Tab.AUTab.settingsContainer);
        Obj = categoryHeaderMasked.gameObject;
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, 2f, -2f);
        categoryHeaderMasked.Title.text = Name;
        categoryHeaderMasked.Title.outlineColor = Color.black;
        categoryHeaderMasked.Title.outlineWidth = 0.2f;
        categoryHeaderMasked.Title.fontSizeMax = 5f;
        categoryHeaderMasked.Background.material.SetInt(PlayerMaterial.MaskLayer, MaskLayer);
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, MaskLayer);
        }
        categoryHeaderMasked.Title.fontMaterial.SetFloat("_StencilComp", 3f);
        categoryHeaderMasked.Title.fontMaterial.SetFloat("_Stencil", MaskLayer);
        Tab.Children.Add(this);
    }

    internal override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (categoryHeaderMasked != null)
        {
            categoryHeaderMasked.Title.text = Name;
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

using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

internal class OptionDividerItem : OptionItem
{
    internal override bool CanLoad => false;
    internal override bool IsOption => false;
    internal (float top, float bottom) Distance { get; set; }

    /// <summary>
    /// Creates a new divider item for the options menu. The divider is placed within the specified tab with configurable top and bottom distances.
    /// </summary>
    /// <param name="tab">The tab where the divider item will be placed.</param>
    /// <param name="topDistance">The distance from the top of the tab to the divider. Default is 0.26f.</param>
    /// <param name="bottomDistance">The distance from the bottom of the tab to the divider. Default is 0.50f.</param>
    /// <returns>The created <see cref="OptionDividerItem"/> instance.</returns>
    internal static OptionDividerItem Create(OptionTab tab, float topDistance = 0.26f, float bottomDistance = 0.50f)
    {
        var Item = new OptionDividerItem
        {
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
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate(Tab.AUTab.categoryHeaderOrigin, Tab.AUTab.settingsContainer);
        Obj = categoryHeaderMasked.gameObject;
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.Background.gameObject.DestroyObj();
        categoryHeaderMasked.Title.DestroyObj();
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, MaskLayer);
        }
        Tab.Children.Add(this);
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

using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionDividerItem : BetterOptionItem
{
    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu, float topDistance = 0.15f, float bottomDistance = 0.50f)
    {
        if (gameOptionsMenu.Tab == null)
        {
            return this;
        }

        Id = -1;
        Tab = gameOptionsMenu;
        this.topDistance = topDistance;
        this.bottomDistance = bottomDistance;
        Name = "Divider";
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, 2f, -2f);
        categoryHeaderMasked.Background.gameObject.DestroyObj();
        categoryHeaderMasked.Title.DestroyObj();
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }

        BetterOptionItems.Add(this);
        obj = categoryHeaderMasked.gameObject;

        return this;
    }
}

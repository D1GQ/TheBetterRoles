using UnityEngine;

namespace TheBetterRoles;

public class BetterOptionDividerItem : BetterOptionItem
{
    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu)
    {
        if (gameOptionsMenu.Tab == null)
        {
            return this;
        }

        Tab = gameOptionsMenu;
        Name = "Divider";
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate<CategoryHeaderMasked>(gameOptionsMenu.Tab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, 2f, -2f);
        UnityEngine.Object.Destroy(categoryHeaderMasked.Background.gameObject);
        UnityEngine.Object.Destroy(categoryHeaderMasked.Title.gameObject);
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }

        BetterOptionItems.Add(this);
        obj = categoryHeaderMasked.gameObject;

        return this;
    }
}

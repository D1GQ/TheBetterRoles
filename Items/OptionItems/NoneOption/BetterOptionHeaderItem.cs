using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionHeaderItem : BetterOptionItem
{
    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu, string name)
    {
        if (gameOptionsMenu.Tab == null)
        {
            return this;
        }

        Tab = gameOptionsMenu;
        Name = name;
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, 2f, -2f);
        categoryHeaderMasked.Title.text = name;
        categoryHeaderMasked.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }
        categoryHeaderMasked.Title.fontMaterial.SetFloat("_StencilComp", 3f);
        categoryHeaderMasked.Title.fontMaterial.SetFloat("_Stencil", maskLayer);

        BetterOptionItems.Add(this);
        obj = categoryHeaderMasked.gameObject;

        return this;
    }
}

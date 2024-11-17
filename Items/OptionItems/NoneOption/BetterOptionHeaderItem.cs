using UnityEngine;

namespace TheBetterRoles.Items.OptionItems;

public class BetterOptionHeaderItem : BetterOptionItem
{
    public BetterOptionItem Create(BetterOptionTab gameOptionsMenu, string name, float topDistance = 0.1f, float bottomDistance = 0.75f)
    {
        if (gameOptionsMenu.Tab == null)
        {
            return this;
        }

        Id = -1;
        Tab = gameOptionsMenu;
        Name = $"<b>{name}</b>";
        this.topDistance = topDistance;
        this.bottomDistance = bottomDistance;
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate(gameOptionsMenu.Tab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, gameOptionsMenu.Tab.settingsContainer);
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.transform.localPosition = new Vector3(-0.903f, 2f, -2f);
        categoryHeaderMasked.Title.text = name;
        categoryHeaderMasked.Title.outlineColor = Color.black;
        categoryHeaderMasked.Title.outlineWidth = 0.2f;
        categoryHeaderMasked.Title.fontSizeMax = 3f;
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

using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class CustomSkinAnimator : MonoBehaviour
{
    internal SkinLayer? skinLayer;

    private void OnWillRenderObject()
    {
        if (skinLayer == null) return;

        if (skinLayer.data == null || !CustomHatManager.SkinsCacheProdId.Contains(skinLayer.data.ProductId)) return;

        if (skinLayer.skin != null && skinLayer.skin.IdleFrame != null)
        {
            if (!GameState.IsExilling)
            {
                Utils.ReplaceTexture(skinLayer.layer, skinLayer.skin.IdleFrame.texture);
            }
            else
            {
                skinLayer.layer.sprite = null;
            }
        }
    }
}
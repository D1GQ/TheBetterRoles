using UnityEngine;

namespace TheBetterRoles.Modules;

internal class RainbowColorEffect : ColorEffect
{
    internal override Color MainColor => MultiColorGradient([], 0.8f);

    internal override void SetEffect(Renderer rend, bool bodyMaterial)
    {
        SetDefaultEffects(rend, bodyMaterial);

        if (bodyMaterial)
        {
            SetVisor(rend);
        }
    }
}
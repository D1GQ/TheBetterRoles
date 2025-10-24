using UnityEngine;

namespace TheBetterRoles.Modules;

internal class WaterColorEffect : ColorEffect
{
    internal override Color MainColor => MultiColorGradient([
        new Color(0f, 0.34f, 0.64f),
        new Color(0.68f, 0.85f, 1f),
    ], 1.8f);

    internal override void SetEffect(Renderer rend, bool bodyMaterial)
    {
        SetDefaultEffects(rend, bodyMaterial);

        if (bodyMaterial)
        {
            SetVisor(rend);
        }
    }
}
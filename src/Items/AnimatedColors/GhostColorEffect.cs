using UnityEngine;

namespace TheBetterRoles.Modules;

internal class GhostColorEffect : ColorEffect
{
    internal override Color MainColor => MultiColorGradient([
        new Color(1f, 1f, 1f),
        new Color(0.45f, 0.45f, 0.45f),
    ], 3.5f);
    internal override Color ShadowColor => MainColor;

    internal override void SetEffect(Renderer rend, bool bodyMaterial)
    {
        SetDefaultEffects(rend, bodyMaterial);

        if (bodyMaterial)
        {
            SetVisor(rend);
        }
    }
}
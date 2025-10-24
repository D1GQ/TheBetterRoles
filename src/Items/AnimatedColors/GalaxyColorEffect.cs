using UnityEngine;

namespace TheBetterRoles.Modules;

internal class GalaxyColorEffect : ColorEffect
{
    internal override Color MainColor => MultiColorGradient([
        new Color(0.05f, 0f, 0.27f),
        new Color(0.60f, 0f, 1f),
        new Color(1f, 0f, 0.97f),
        new Color(0.60f, 0f, 1f)], 2.5f);

    internal override void SetEffect(Renderer rend, bool bodyMaterial)
    {
        SetDefaultEffects(rend, bodyMaterial);

        if (bodyMaterial)
        {
            SetVisor(rend);
        }
    }
}
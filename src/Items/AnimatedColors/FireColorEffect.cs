using UnityEngine;

namespace TheBetterRoles.Modules;

internal class FireColorEffect : ColorEffect
{
    internal override Color MainColor => MultiColorGradient([
        new Color(1f, 0.5f, 0f),
        Color.red,
        new Color(0.5f, 0f, 0f)], 0.28f);

    internal override void SetEffect(Renderer rend, bool bodyMaterial)
    {
        SetDefaultEffects(rend, bodyMaterial);

        if (bodyMaterial)
        {
            SetVisor(rend);
        }
    }
}
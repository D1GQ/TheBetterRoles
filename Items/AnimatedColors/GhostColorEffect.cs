using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Modules;

public class GhostColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.GhostId;
    public override Color MainColor => MultiColorGradient([
        new Color(1f, 1f, 1f),
        new Color(0.45f, 0.45f, 0.45f),
    ], 3.5f);
    public override Color ShadowColor => MainColor;
    public override Color VisorColor => MainColor;

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        SetDefaultEffects(rend, colorId, isBodyMaterial);
    }
}
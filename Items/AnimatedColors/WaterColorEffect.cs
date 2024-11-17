using UnityEngine;

namespace TheBetterRoles.Modules;

public class WaterColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.WaterId;
    public override Color MainColor => MultiColorGradient([
        new Color(0f, 0.34f, 0.64f),
        new Color(0.68f, 0.85f, 1f),
    ], 1.8f);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        SetDefaultEffects(rend, colorId, isBodyMaterial);
    }
}
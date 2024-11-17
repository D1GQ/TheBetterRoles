using UnityEngine;

namespace TheBetterRoles.Modules;

public class RainbowColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.RainbowId;
    public override Color MainColor => MultiColorGradient([], 0.8f);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        SetDefaultEffects(rend, colorId, isBodyMaterial);
    }
}
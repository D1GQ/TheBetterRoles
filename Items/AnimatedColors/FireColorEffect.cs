using UnityEngine;

namespace TheBetterRoles.Modules;

public class FireColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.FireId;
    public override Color MainColor => MultiColorGradient([
        new Color(1f, 0.5f, 0f),
        Color.red,
        new Color(0.5f, 0f, 0f)], 0.28f);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        SetDefaultEffects(rend, colorId, isBodyMaterial);
    }
}
using UnityEngine;

namespace TheBetterRoles.Modules;

public class GalaxyColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.GalaxyId;
    public override Color MainColor => MultiColorGradient([
        new Color(0.05f, 0f, 0.27f),
        new Color(0.60f, 0f, 1f),
        new Color(1f, 0f, 0.97f),
        new Color(0.60f, 0f, 1f)], 2.5f);
    public override Color VisorColor => Visor(MainColor);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        SetDefaultEffects(rend, colorId, isBodyMaterial);
    }
}
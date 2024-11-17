using UnityEngine;

namespace TheBetterRoles.Modules;

public class RainbowColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.RainbowId;
    public static Color Rainbow => MultiColorGradient([], 0.8f);
    public static Color RainbowShadow => Shadow(Rainbow);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        if (isBodyMaterial)
        {
            rend.material.SetColor(PlayerMaterial.BackColor, RainbowShadow);
            rend.material.SetColor(PlayerMaterial.BodyColor, Rainbow);
            rend.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
        else
        {
            rend.material.SetColor("_Color", Rainbow);
        }
    }
}
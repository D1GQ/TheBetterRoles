using UnityEngine;

namespace TheBetterRoles.Modules;

public class WaterColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.WaterId;
    public static Color Water => MultiColorGradient([
        new Color(0f, 0.34f, 0.64f),
        new Color(0.68f, 0.85f, 1f),
    ], 1.8f);
    public static Color WaterShadow => Shadow(Water);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        if (isBodyMaterial)
        {
            rend.material.SetColor(PlayerMaterial.BackColor, WaterShadow);
            rend.material.SetColor(PlayerMaterial.BodyColor, Water);
            rend.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
        else
        {
            rend.material.SetColor("_Color", Water);
        }
    }
}
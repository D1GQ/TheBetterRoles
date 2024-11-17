using UnityEngine;

namespace TheBetterRoles.Modules;

public class FireColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.FireId;
    public static Color Fire => MultiColorGradient([
        new Color(1f, 0.5f, 0f),
        Color.red,
        new Color(0.5f, 0f, 0f)], 0.28f);
    public static Color FireShadow => Shadow(Fire);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        if (isBodyMaterial)
        {
            rend.material.SetColor(PlayerMaterial.BackColor, FireShadow);
            rend.material.SetColor(PlayerMaterial.BodyColor, Fire);
            rend.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
        else
        {
            rend.material.SetColor("_Color", Fire);
        }
    }
}
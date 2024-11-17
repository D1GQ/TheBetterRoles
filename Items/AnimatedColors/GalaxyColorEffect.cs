using UnityEngine;

namespace TheBetterRoles.Modules;

public class GalaxyColorEffect : ColorEffect
{
    public override int EffectId => CustomColors.GalaxyId;
    public static Color Galaxy => MultiColorGradient([
        new Color(0.05f, 0f, 0.27f),
        new Color(0.60f, 0f, 1f),
        new Color(1f, 0f, 0.97f),
        new Color(0.60f, 0f, 1f)], 2.5f);
    public static Color GalaxyShadow => Shadow(Galaxy);
    public static Color GalaxyVisor => Visor(Galaxy);

    public override void SetEffect(Renderer rend, int colorId, bool isBodyMaterial)
    {
        if (isBodyMaterial)
        {
            rend.material.SetColor(PlayerMaterial.BackColor, GalaxyShadow);
            rend.material.SetColor(PlayerMaterial.BodyColor, Galaxy);
            rend.material.SetColor(PlayerMaterial.VisorColor, GalaxyVisor);
        }
        else
        {
            rend.material.SetColor("_Color", Galaxy);
        }
    }
}
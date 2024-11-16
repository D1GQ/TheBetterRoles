using UnityEngine;

namespace TheBetterRoles.Modules;

public class ColorEffectBehaviour : MonoBehaviour
{
    public static List<ColorEffectBehaviour> effects = [];
    public Renderer Renderer;
    public int Id;
    public bool IsBodyMaterial;

    public static Color Rainbow => MultiColorGradient([], 0.8f);
    public static Color RainbowShadow => Shadow(Rainbow);

    public static Color Galaxy => MultiColorGradient([
        new Color(0.05f, 0f, 0.27f),
        new Color(0.60f, 0f, 1f),
        new Color(1f, 0f, 0.97f),
        new Color(0.60f, 0f, 1f)], 2.5f);
    public static Color GalaxyShadow => Shadow(Galaxy);
    public static Color GalaxyVisor => Visor(Galaxy);

    public static Color Fire => MultiColorGradient([
        new Color(1f, 0.5f, 0f),
        Color.red,
        new Color(0.5f, 0f, 0f)], 0.28f);
    public static Color FireShadow => Shadow(Fire);

    public static Color MultiColorGradient(Color[] colors, float speed)
    {
        if (colors == null || colors.Length == 0)
        {
            colors = [
            Color.red,
            new Color(1f, 0.5f, 0f),
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta
        ];
        }

        if (colors.Length < 2)
            throw new ArgumentException("Provide at least two colors for the gradient.");

        float totalTime = colors.Length * speed;

        float cycleTime = Time.time % totalTime;

        int currentIndex = Mathf.FloorToInt(cycleTime / speed);
        int nextIndex = (currentIndex + 1) % colors.Length;

        float blendFactor = (cycleTime % speed) / speed;

        return Color.Lerp(colors[currentIndex], colors[nextIndex], blendFactor);
    }

    public static Color Shadow(Color color)
    {
        return new Color(color.r - 0.3f, color.g - 0.3f, color.b - 0.3f);
    }

    public static Color Visor(Color color)
    {
        return new Color(color.r + 0.5f, color.g + 0.5f, color.b + 0.5f);
    }

    public static bool UsedColor(Renderer rend, int colorId)
    {
        var eff = effects.FirstOrDefault(e => e.Renderer == rend);
        if (eff != null)
        {
            return colorId == eff.Id;
        }
        return false;
    }

    public void AddRend(Renderer rend, int colorId, bool isBodyMaterial = true)
    {
        if (UsedColor(rend, colorId))
        {
            Destroy(this);
            return;
        }
        else
        {
            TryRemove(rend);
        }

        Renderer = rend;
        Id = colorId;
        IsBodyMaterial = isBodyMaterial;
        effects.Add(this);
    }

    public static void TryRemove(Renderer rend)
    {
        var eff = effects.FirstOrDefault(e => e.Renderer == rend);
        if (eff != null)
        {
            effects.Remove(eff);
            Destroy(eff);
        }
    }

    public void Update()
    {
        if (Renderer == null)
        {
            Destroy(this);
            return;
        }
        if (!Renderer.enabled || !Renderer.gameObject.active)
        {
            return;
        }

        switch (Id)
        {
            case CustomColors.RainbowId:
                SetRainbow();
                break;
            case CustomColors.GalaxyId:
                SetGalaxy();
                break;
            case CustomColors.FireId:
                SetFire();
                break;
        }
    }

    public void SetRainbow()
    {
        if (IsBodyMaterial)
        {
            Renderer.material.SetColor(PlayerMaterial.BackColor, RainbowShadow);
            Renderer.material.SetColor(PlayerMaterial.BodyColor, Rainbow);
            Renderer.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
        else
        {
            Renderer.material.SetColor("_Color", Rainbow);
        }
    }

    public void SetGalaxy()
    {
        if (IsBodyMaterial)
        {
            Renderer.material.SetColor(PlayerMaterial.BackColor, GalaxyShadow);
            Renderer.material.SetColor(PlayerMaterial.BodyColor, Galaxy);
            Renderer.material.SetColor(PlayerMaterial.VisorColor, GalaxyVisor);
        }
        else
        {
            Renderer.material.SetColor("_Color", Galaxy);
        }
    }

    public void SetFire()
    {
        if (IsBodyMaterial)
        {
            Renderer.material.SetColor(PlayerMaterial.BackColor, FireShadow);
            Renderer.material.SetColor(PlayerMaterial.BodyColor, Fire);
            Renderer.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
        else
        {
            Renderer.material.SetColor("_Color", Fire);
        }
    }

    public ColorEffectBehaviour(IntPtr ptr) : base(ptr) { }
}
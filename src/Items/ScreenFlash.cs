using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Managers;
using UnityEngine;

namespace TheBetterRoles.Items;

internal class ScreenFlash
{
    internal static List<ScreenFlash> ScreenFlashs = [];

    internal string Name { get; set; }
    internal SpriteRenderer FullScreen { get; set; }
    internal void Create(string name, Color color, float fadeInDuration = 0.25f, float fadeOutDuration = 0.25f, float effectDuration = 1f, bool Override = false, bool fullColor = false)
    {
        if (Override)
        {
            Stop(name);
        }
        else if (ScreenFlashs.FirstOrDefault(x => x.Name == name) != null)
        {
            return;
        }
        Name = name;
        var hud = HudManager.Instance;
        if (hud.FullScreen == null) return;
        FullScreen = UnityEngine.Object.Instantiate(hud.FullScreen, hud.transform);
        FullScreen.name = $"{name}_FullScreen";
        ScreenFlashs.Add(this);
        AnimateCoroutine = CoroutineManager.Instance.StartCoroutine(CoAnimate(color, fadeInDuration, fadeOutDuration, effectDuration, fullColor));
    }

    internal Coroutine AnimateCoroutine { get; set; }
    private IEnumerator CoAnimate(Color color, float fadeInDuration, float fadeOutDuration, float effectDuration, bool fullColor = false)
    {
        if (FullScreen == null) yield break;

        effectDuration = Mathf.Max(0, effectDuration - (fadeInDuration + fadeOutDuration));

        FullScreen.gameObject.SetActive(true);
        FullScreen.color = new Color(color.r, color.g, color.b, 0);

        float targetAlpha = fullColor ? color.a : color.a / 2;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, targetAlpha, elapsed / fadeInDuration);
            FullScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        FullScreen.color = new Color(color.r, color.g, color.b, targetAlpha);

        yield return new WaitForSeconds(effectDuration);

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(targetAlpha, 0, elapsed / fadeOutDuration);
            FullScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        FullScreen.color = new Color(color.r, color.g, color.b, 0);

        FullScreen.gameObject.SetActive(false);
        AnimateCoroutine = null;
        Remove();
    }

    internal void Remove()
    {
        if (AnimateCoroutine != null)
        {
            CoroutineManager.Instance.StopCoroutine(AnimateCoroutine);
        }
        UnityEngine.Object.Destroy(FullScreen);
        ScreenFlashs.Remove(this);
    }

    internal static void Stop(string name)
    {
        var flash = ScreenFlashs.FirstOrDefault(x => x.Name == name);
        flash?.Remove();
    }
}

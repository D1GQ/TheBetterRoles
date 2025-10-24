using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Patches.Manager;

[HarmonyPatch(typeof(MainMenuManager))]
internal class MainMenuManagerPatch
{
    private static GameObject? logo;

    [HarmonyPatch(nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    private static void Postfix(MainMenuManager __instance)
    {
        if (logo == null)
        {
            GameObject.Find("Ambience/PlayerParticles").SetActive(false);
            logo = new GameObject("TheBetterRoles_Logo");
            var sprite = logo.AddComponent<SpriteRenderer>();
            var aspect = logo.AddComponent<AspectPosition>();
            sprite.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.TheBetterRoles.png", 150);
            aspect.Alignment = AspectPosition.EdgeAlignments.Right;
            aspect.DistanceFromEdge = new Vector3(3.25f, -0.25f, 0f);
            aspect.AdjustPosition();
        }
    }

    private static SpriteRenderer? sprite;

    [HarmonyPatch(nameof(MainMenuManager.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdate_Postfix(MainMenuManager __instance)
    {
        if (BannedUserDataExtensions.IsBanned || FileChecker.HasUnauthorizedFileOrMod)
        {
            __instance.playButton.enabled = false;
            sprite ??= __instance.playButton.transform.Find("Inactive").GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = new Color(0.7f, 0.7f, 0.7f);
            }

            SceneManager.s_AllowLoadScene = false;
        }
        else
        {
            __instance.playButton.enabled = true;
            sprite ??= __instance.playButton.transform.Find("Inactive").GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = Color.white;
            }

            SceneManager.s_AllowLoadScene = true;
        }

        bool Flag = __instance?.screenTint?.GetComponent<SpriteRenderer>() != null
            && !__instance.screenTint.GetComponent<SpriteRenderer>().enabled;

        logo.SetActive(Flag);
    }
}

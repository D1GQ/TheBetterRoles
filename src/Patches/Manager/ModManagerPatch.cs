using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Manager;

[HarmonyPatch(typeof(ModManager))]
internal class ModManagerPatch
{
    private static SpriteRenderer? DownloadIcon;
    private static float _alpha = 1;

    [HarmonyPatch(nameof(ModManager.LateUpdate))]
    [HarmonyPrefix]
    private static bool LateUpdate_Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

        if (DownloadIcon == null)
        {
            DownloadIcon = UnityEngine.Object.Instantiate(__instance.ModStamp, __instance.transform);
            DownloadIcon.sprite = Utils.LoadSprite($"TheBetterRoles.Resources.Images.Icons.Downloading.png", 250);
            DownloadIcon.name = "DownloadIcon";
        }
        else
        {
            DownloadIcon.transform.localPosition = __instance.ModStamp.transform.localPosition + new Vector3(-0.7f, 0f, 1f);
            bool isDownloading = !GithubAPI.DownloadingContentQueue;
            DownloadIcon.enabled = isDownloading;
            if (isDownloading)
            {
                _alpha = Mathf.PingPong(Time.time * 0.5f, 0.5f) + 0.25f;
                DownloadIcon.color = new Color(DownloadIcon.color.r, DownloadIcon.color.g, DownloadIcon.color.b, _alpha);
            }
        }

        bool show = GameState.IsInGame;

        if (show && HudManager.InstanceExists)
        {
            var hudManager = HudManager.Instance;
            show = hudManager != null && hudManager.shhhEmblem.gameObject.active
                || hudManager != null && hudManager.discussEmblem.gameObject.active
                || hudManager != null && hudManager.GameLoadAnimation.active
                || MeetingHud.Instance?.state == MeetingHud.VoteStates.Animating;

            show &= hudManager != null && !hudManager.MapButton.gameObject.active;
        }

        __instance.ModStamp.enabled = !GameState.IsInGame || GameState.IsInIntro || GameState.IsExilling || show;



        if (!__instance.ModStamp.enabled)
        {
            return false;
        }
        if (!__instance.localCamera)
        {
            if (HudManager.InstanceExists)
            {
                __instance.localCamera = HudManager.Instance.GetComponentInChildren<Camera>();
            }
            else
            {
                __instance.localCamera = Camera.main;
            }
        }
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(__instance.localCamera, AspectPosition.EdgeAlignments.RightTop, new Vector3(0.4f, 0.4f, __instance.localCamera.nearClipPlane + 0.1f));

        return false;
    }
}

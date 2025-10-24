using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using TheBetterRoles.Items;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class ExtendedPlayerControl : MonoBehaviour, IMonoExtension<PlayerControl>
{
    public PlayerControl? BaseMono { get; set; }
    internal TextMeshPro? InfoTextInfo { get; set; }
    internal TextMeshPro? InfoTextTop { get; set; }
    internal TextMeshPro? InfoTextBottom { get; set; }

    internal bool InteractableTarget { get; set; } = true;
    internal bool IsFake { get; set; }
    [HideFromIl2Cpp]
    internal BoolQueue CanRepairSabotageQueue { get; } = new(true);
    [HideFromIl2Cpp]
    internal BoolQueue InteractableTargetQueue { get; } = new(true);
    [HideFromIl2Cpp]
    internal BoolQueue PlayerTextActiveQueue { get; } = new();
    [HideFromIl2Cpp]
    internal BoolQueue CamouflagedQueue { get; } = new();
    [HideFromIl2Cpp]
    internal BoolQueue CosmeticsActiveQueue { get; } = new();
    internal int CamouflageBackToColor { get; set; } = 0;

    private void Awake()
    {
        if (!this.RegisterExtension()) return;
        this.StartCoroutine(CoAddExtendedData());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoAddExtendedData()
    {
        while (BaseMono.Data == null)
        {
            yield return null;
        }

        BaseMono.Data.gameObject.AddComponent<ExtendedPlayerInfo>();
    }

    private void OnDestroy()
    {
        this.UnregisterExtension();
    }
}

internal static class PlayerControlExtension
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.Awake))]
        [HarmonyPostfix]
        internal static void Awake_Postfix(PlayerControl __instance)
        {
            var nameTextTransform = __instance.gameObject.transform.Find("Names/NameText_TMP");
            var nameText = nameTextTransform?.GetComponent<TextMeshPro>();

            TextMeshPro InstantiatePlayerInfoText(string name, Vector3 positionOffset)
            {
                var newTextObject = UnityEngine.Object.Instantiate(nameText, nameTextTransform);
                newTextObject.name = name;
                newTextObject.transform.DestroyChildren();
                newTextObject.transform.position += positionOffset;
                var textMesh = newTextObject.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.text = string.Empty;
                }
                newTextObject.gameObject.SetActive(true);
                return newTextObject;
            }

            var text1 = InstantiatePlayerInfoText("InfoText_Info_TMP", new Vector3(0f, 0.25f));
            var text2 = InstantiatePlayerInfoText("InfoText_T_TMP", new Vector3(0f, 0.15f));
            var text3 = InstantiatePlayerInfoText("InfoText_B_TMP", new Vector3(0f, -0.15f));

            TryCreateExtendedPlayerControl(__instance, text1, text2, text3);
        }

        internal static void TryCreateExtendedPlayerControl(PlayerControl pc, TextMeshPro InfoText_Info_TMP, TextMeshPro InfoText_T_TMP, TextMeshPro InfoText_B_TMP)
        {
            if (pc.ExtendedPC() == null)
            {
                ExtendedPlayerControl newExtendedPc = pc.gameObject.AddComponent<ExtendedPlayerControl>();
                newExtendedPc.InfoTextInfo = InfoText_Info_TMP;
                newExtendedPc.InfoTextTop = InfoText_T_TMP;
                newExtendedPc.InfoTextBottom = InfoText_B_TMP;
            }
        }
    }

    /// <summary>
    /// Retrieves the <see cref="ExtendedPC"/> component from the given player.
    /// </summary>
    /// <param name="player">The player to get the extended control from.</param>
    /// <returns>The <see cref="ExtendedPC"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerControl? ExtendedPC(this PlayerControl player)
    {
        return MonoExtensionManager.Get<ExtendedPlayerControl>(player);
    }

    /// <summary>
    /// Retrieves the <see cref="ExtendedPC"/> component from the given player physics instance.
    /// </summary>
    /// <param name="playerPhysics">The player physics component to get the extended control from.</param>
    /// <returns>The <see cref="ExtendedPC"/> component if found; otherwise, null.</returns>
    internal static ExtendedPlayerControl? ExtendedPC(this PlayerPhysics playerPhysics)
    {
        return MonoExtensionManager.Get<ExtendedPlayerControl>(playerPhysics.myPlayer);
    }
}
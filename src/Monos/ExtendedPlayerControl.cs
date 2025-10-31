using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using TheBetterRoles.Items;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class ExtendedPlayerControl : MonoBehaviour, IMonoExtension<PlayerControl>
{
    public PlayerControl? BaseMono { get; set; }

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
            TryCreateExtendedPlayerControl(__instance);
        }

        internal static void TryCreateExtendedPlayerControl(PlayerControl pc)
        {
            if (pc.ExtendedPC() == null)
            {
                ExtendedPlayerControl newExtendedPc = pc.gameObject.AddComponent<ExtendedPlayerControl>();
                pc.gameObject.AddComponent<PlayerInfoDisplay>().Init(pc);
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
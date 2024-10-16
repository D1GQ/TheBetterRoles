using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ShapeshifterMinigame))]
class ShapeshifterMinigamePatch
{
    [HarmonyPatch(nameof(ShapeshifterMinigame.Begin))]
    [HarmonyPostfix]
    public static void Begin_Postfix(ShapeshifterMinigame __instance, [HarmonyArgument(0)] PlayerTask task)
    {
        __instance.Begin(task);
        List<byte> bodies = [];
        UnityEngine.Object.FindObjectsOfType<DeadBody>().ToList().ForEach(body => bodies.Add(body.ParentId));
        List<PlayerControl> list = Main.AllPlayerControls.Where(p => p != PlayerControl.LocalPlayer && (!p.Data.IsDead || (p.Data.IsDead && bodies.Contains(p.PlayerId)))).ToList();
        __instance.potentialVictims = new Il2CppSystem.Collections.Generic.List<ShapeshifterPanel>();
        Il2CppSystem.Collections.Generic.List<UiElement> list2 = new Il2CppSystem.Collections.Generic.List<UiElement>();
        for (int i = 0; i < list.Count; i++)
        {
            PlayerControl player = list[i];
            int num = i % 3;
            int num2 = i / 3;
            ShapeshifterPanel shapeshifterPanel = UnityEngine.Object.Instantiate(__instance.PanelPrefab, __instance.transform);
            shapeshifterPanel.transform.localPosition = new Vector3(__instance.XStart + num * __instance.XOffset, __instance.YStart + (float)num2 * __instance.YOffset, -1f);
            shapeshifterPanel.SetPlayer(i, player.Data, (Action)(() =>
            {
                PlayerControl.LocalPlayer.PlayerMenuSync(player, __instance);
            }));
            __instance.potentialVictims.Add(shapeshifterPanel);
            list2.Add(shapeshifterPanel.Button);
        }
        ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.DefaultButtonSelected, list2, false);
    }
}
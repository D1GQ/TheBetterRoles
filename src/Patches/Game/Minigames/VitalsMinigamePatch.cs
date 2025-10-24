using HarmonyLib;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Minigames;

[HarmonyPatch(typeof(VitalsMinigame))]
internal class VitalsMinigamePatch
{
    [HarmonyPatch(nameof(VitalsMinigame.Begin))]
    [HarmonyPrefix]
    private static bool Begin_Prefix(VitalsMinigame __instance, ref PlayerTask task)
    {
        Minigame.Instance = __instance;
        __instance.MyTask = task;
        __instance.MyNormTask = task as NormalPlayerTask;
        __instance.timeOpened = Time.realtimeSinceStartup;
        if (PlayerControl.LocalPlayer)
        {
            if (MapBehaviour.Instance)
            {
                MapBehaviour.Instance.Close();
            }
            PlayerControl.LocalPlayer.MyPhysics.SetNormalizedVelocity(Vector2.zero);
        }
        __instance.StartCoroutine(__instance.CoAnimateOpen());

        NetworkedPlayerInfo[] allData = GameData.Instance.AllPlayers.ToArray().OrderBy(data => data.PlayerId).ToArray();
        List<VitalsPanel> vitalsPanels = [];
        foreach (var data in allData)
        {
            VitalsPanel vitalsPanel = UnityEngine.Object.Instantiate(__instance.PanelPrefab, __instance.transform);
            vitalsPanel.PlayerInfo = data;

            if ((!data.IsAlive(true) || data.Disconnected) && data.DeadBody() == null)
            {
                vitalsPanel.SetDisconnected();
            }
            else if (!data.IsAlive())
            {
                vitalsPanel.SetDead();
            }

            vitalsPanels.Add(vitalsPanel);
        }

        int index = 0;

        foreach (var panel in vitalsPanels.OrderBy(pan => !pan.IsDiscon ? 0 : 1))
        {
            int num = index % 3;
            int num2 = index / 3;
            panel.transform.localPosition = new Vector3(__instance.XStart + num * __instance.XOffset, __instance.YStart + num2 * __instance.YOffset, -1f);
            panel.SetPlayer(index, panel.PlayerInfo);

            index++;
        }

        __instance.vitals = vitalsPanels.ToArray();

        return false;
    }

    [HarmonyPatch(nameof(VitalsMinigame.Update))]
    [HarmonyPostfix]
    private static void Update_Postfix(VitalsMinigame __instance)
    {
        foreach (var vitalsPanel in __instance.vitals)
        {
            if (!vitalsPanel.IsDiscon && (!vitalsPanel.PlayerInfo.IsAlive(true) || vitalsPanel.PlayerInfo.Disconnected) && vitalsPanel.PlayerInfo.DeadBody() == null)
            {
                vitalsPanel.SetDisconnected();
            }
            else if (!vitalsPanel.IsDead && !vitalsPanel.IsDiscon && !vitalsPanel.PlayerInfo.IsAlive(true))
            {
                vitalsPanel.SetDead();
            }
        }
    }
}
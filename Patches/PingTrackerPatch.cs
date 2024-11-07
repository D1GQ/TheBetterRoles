using HarmonyLib;
using System.Text;
using TheBetterRoles.Modules;
using UnityEngine;
using static TheBetterRoles.Patches.GamePlayManager;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(PingTracker))]
public class PingTrackerPatch
{
    [HarmonyPatch(nameof(PingTracker.Update))]
    [HarmonyPrefix]
    public static bool Prefix(PingTracker __instance)
    {
        try
        {
            if (GameState.IsFreePlay)
            {
                __instance.gameObject.SetActive(false);
            }

            StringBuilder sb = new();

            sb.AppendFormat("{0}: <b>{1}</b>\n", Translator.GetString("Ping").ToUpper(), GetPingColor(AmongUsClient.Instance.Ping));

            if (GameState.IsLobby && GameState.IsHost && GameState.IsVanillaServer && !GameState.IsLocalGame)
            {
                sb.AppendFormat("{0}: <b>{1}</b>\n", Translator.GetString("Timer").ToUpper(), GetTimeColor(GameStartManagerPatch.lobbyTimer));
            }

            sb.Append($"<color=#00dbdb><size=75%>TheBetterRoles {Main.GetVersionText(true)}</size></color>\n");
            sb.Append("<size=68%><color=#8040bf>By</color> <color=#bc4345>The Enhanced Network</color></size>\n");

            // sb.Append($"<size=50%><color=#b5b5b5>{Main.Github}</color></size>\n");

            if (Main.ShowFPS.Value)
            {
                float FPSNum = 1.0f / Time.deltaTime;
                sb.AppendFormat("<color=#0dff00><size=75%>FPS: <b>{0}</b></size></color>\n", (int)FPSNum);
            }

            __instance.aspectPosition.DistanceFromEdge = new Vector3(4f, 0.1f, -5);
            __instance.aspectPosition.Alignment = AspectPosition.EdgeAlignments.RightTop;
            __instance.text.outlineWidth = 0.3f;
            __instance.text.text = sb.ToString();
        }
        catch { }

        return false;
    }

    private static string GetPingColor(int ping)
    {
        string color;
        switch (ping)
        {
            case int n when n > 250:
                color = "#ff0000";
                break;
            case int n when n > 100:
                color = "#ffa200";
                break;
            case int n when n > 50:
                color = "#ffff00";
                break;
            default:
                color = "#00f04c";
                break;
        }

        string newPing = $"<color={color}>{ping} ms</color>";

        return newPing;
    }

    private static string GetTimeColor(float time)
    {
        float minutes = (int)time / 60 + 1f;

        string color;
        switch (minutes)
        {
            case float n when n <= 1:
                color = "#ff0000";
                break;
            case float n when n <= 3.5f:
                color = "#ffa200";
                break;
            case float n when n <= 5f:
                color = "#ffff00";
                break;
            default:
                color = "#00f04c";
                break;
        }

        string newTime = $"<color={color}>{GameStartManagerPatch.lobbyTimerDisplay}</color>";

        return newTime;
    }
}

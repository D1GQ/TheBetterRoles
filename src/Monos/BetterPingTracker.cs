using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class BetterPingTracker : MonoBehaviour
{
    internal static BetterPingTracker? Instance { get; private set; }
    private AspectPosition? aspectPosition;
    public TextMeshPro? text;
    internal void SetUp(TextMeshPro pingText, AspectPosition pingAspectPosition)
    {
        if (Instance != null) return;
        Instance = this;
        text = pingText;
        aspectPosition = pingAspectPosition;
    }

    private void Update()
    {
        aspectPosition.DistanceFromEdge = new Vector3(4f, 0.1f, -5);
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.RightTop;
        text.outlineWidth = 0.3f;

        StringBuilder sb = new();

#if DEBUG_MULTIACCOUNTS
        sb.AppendFormat("(<#800094>MultiAccounts</color>\n)");
#endif

        if (ModInfo.IsGuestBuild)
        {
            sb.AppendFormat("(<#B5B500>GuestBuild</color>)\n");
        }

        if (!GameState.IsFreePlay)
        {
            string pingColor = Colors.Color32ToHex(Colors.LerpColor([Color.green, Color.yellow, new Color(1f, 0.5f, 0f), Color.red], (25, 250), AmongUsClient.Instance.Ping));
            sb.AppendFormat("{0}: <b>{1}</b>\n", Translator.GetString("Ping").ToUpper(), $"<{pingColor}>{AmongUsClient.Instance.Ping}</color>");
        }

        if (GameState.IsLobby && GameState.IsVanillaServer && !GameState.IsLocalGame && CatchedGameData.lobbyTimer > 0f)
        {
            string timeColor = Colors.Color32ToHex(Colors.LerpColor([Color.green, Color.yellow, new Color(1f, 0.5f, 0f), Color.red], (0, 300), CatchedGameData.lobbyTimer, true));
            sb.AppendFormat("{0}: <b>{1}</b>\n", Translator.GetString("Timer").ToUpper(), $"<{timeColor}>{CatchedGameData.lobbyTimerText}</color>");
        }

        sb.Append($"<color=#00dbdb><size=75%>TheBetterRoles {Main.GetVersionText(true)}</size></color>\n");
        sb.Append($"<color=#8A8A8A>{ModInfo.Github}</color>\n".Size(52f));

        // sb.Append($"<size=50%><color=#b5b5b5>{Main.Github}</color></size>\n");

        if (Main.ShowFPS.Value)
        {
            float FPSNum = 1.0f / Time.deltaTime;
            sb.AppendFormat("<color=#0dff00><size=75%>FPS: <b>{0}</b></size></color>\n", (int)FPSNum);
        }

        text.text = sb.ToString();
    }
}
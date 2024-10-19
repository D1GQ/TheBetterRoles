using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles;

// Code from: https://github.com/0xDrMoe/TownofHost-Enhanced
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class Zoom
{
    private static bool resetButtons = false;

    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer.Role() != null)
        {
            if (GameStates.IsCanMove && (!GameStates.IsInGamePlay || !PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Role()?.IsGhostRole == false
                && !PlayerControl.LocalPlayer.BetterData().IsFakeAlive))
            {
                if (Camera.main.orthographicSize > 3.0f)
                    resetButtons = true;

                if (Input.mouseScrollDelta.y > 0 && Camera.main.orthographicSize > 3.0f)
                {
                    SetZoomSize(times: false);
                }
                else if (Input.mouseScrollDelta.y < 0 && (GameStates.IsDead || GameStates.IsFreePlay || GameStates.IsLobby) &&
                            Camera.main.orthographicSize < 18.0f)
                {
                    SetZoomSize(times: true);
                }

                Flag.NewFlag("Zoom");
            }
            else
            {
                Flag.Run(() => SetZoomSize(reset: true), "Zoom");
            }
        }
    }

    private static void SetZoomSize(bool times = false, bool reset = false)
    {
        float size = times ? 1.5f : 1 / 1.5f;

        if (reset)
        {
            Camera.main.orthographicSize = 3.0f;
            HudManager.Instance.UICamera.orthographicSize = 3.0f;
            HudManager.Instance.Chat.transform.localScale = Vector3.one;
            if (GameStates.IsMeeting)
                MeetingHud.Instance.transform.localScale = Vector3.one;
        }
        else
        {
            Camera.main.orthographicSize *= size;
            HudManager.Instance.UICamera.orthographicSize *= size;
        }

        if (resetButtons)
        {
            ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
            resetButtons = false;
        }
    }
}

public static class Flag
{
    private static readonly List<string> oneTimeList = new();
    private static readonly List<string> firstRunList = new();

    public static void Run(Action action, string type, bool firstrun = false)
    {
        if (oneTimeList.Contains(type) || (firstrun && !firstRunList.Contains(type)))
        {
            if (!firstRunList.Contains(type))
                firstRunList.Add(type);

            oneTimeList.Remove(type);
            action();
        }
    }

    public static void NewFlag(string type)
    {
        if (!oneTimeList.Contains(type))
            oneTimeList.Add(type);
    }

    public static void DeleteFlag(string type)
    {
        oneTimeList.Remove(type);
    }
}
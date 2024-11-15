using HarmonyLib;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;


namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HudManager))]
public class HudManagerPatch
{
    private static bool HasBeenWelcomed = false;

    public static GameObject? ButtonsLeft;
    public static GameObject? ButtonsRight;

    [HarmonyPatch(nameof(HudManager.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix(HudManager __instance)
    {
        // Set up buttons
        GameObject leftButton = UnityEngine.Object.Instantiate(__instance.UseButton.gameObject.transform.parent.gameObject, __instance.UseButton.gameObject.transform.parent.transform.parent);
        ButtonsRight = __instance.UseButton.gameObject.transform.parent.gameObject;
        ButtonsRight.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(0.7f, 0.7f, -9f);
        ButtonsRight.GetComponent<AspectPosition>().AdjustPosition();
        ButtonsLeft = leftButton;
        leftButton.name = "BottomLeft";
        leftButton.transform.DestroyChildren();
        leftButton.GetComponent<AspectPosition>().Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        leftButton.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(0.7f, 0.7f, -9f);
        leftButton.GetComponent<AspectPosition>().AdjustPosition();
        var Grid = leftButton.GetComponent<GridArrange>();
        if (Grid != null)
        {
            Grid.Alignment = GridArrange.StartAlign.Right;
            Grid.MaxColumns = 2;
        }

        if (BetterNotificationManager.BAUNotificationManagerObj == null)
        {
            var ChatNotifications = __instance.Chat.chatNotification;
            if (ChatNotifications != null)
            {
                ChatNotifications.timeOnScreen = 1f;
                ChatNotifications.gameObject.SetActive(true);
                GameObject BAUNotification = UnityEngine.Object.Instantiate(ChatNotifications.gameObject);
                BAUNotification.name = "TBRNotification";
                BAUNotification.GetComponent<ChatNotification>().DestroyMono();
                GameObject.Find($"{BAUNotification.name}/Sizer/PoolablePlayer").DestroyObj();
                GameObject.Find($"{BAUNotification.name}/Sizer/ColorText").DestroyObj();
                BAUNotification.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-1.57f, 5.3f, -15f);
                GameObject.Find($"{BAUNotification.name}/Sizer/NameText").transform.localPosition = new Vector3(-3.3192f, -0.0105f);
                BetterNotificationManager.NameText = GameObject.Find($"{BAUNotification.name}/Sizer/NameText").GetComponent<TextMeshPro>();
                UnityEngine.Object.DontDestroyOnLoad(BAUNotification);
                BetterNotificationManager.BAUNotificationManagerObj = BAUNotification;
                BAUNotification.SetActive(false);
                ChatNotifications.timeOnScreen = 0f;
                ChatNotifications.gameObject.SetActive(false);
                BetterNotificationManager.TextArea.enableWordWrapping = true;
                BetterNotificationManager.TextArea.m_firstOverflowCharacterIndex = 0;
                BetterNotificationManager.TextArea.overflowMode = TextOverflowModes.Overflow;
            }
        }

        _ = new LateTask(() =>
        {
            if (!HasBeenWelcomed && GameState.IsInGame && GameState.IsLobby && !GameState.IsFreePlay)
            {
                BetterNotificationManager.Notify($"<b><color=#00751f>{string.Format(Translator.GetString("WelcomeMsg.WelcomeToTBR"), Translator.GetString("TheBetterRoles"))}!</color></b>", 8f);
                HasBeenWelcomed = true;
            }
        }, 1f, "HudManagerPatch Start");
    }
    [HarmonyPatch(nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(HudManager __instance)
    {
        try
        {
            GameObject gameStart = GameObject.Find("GameStartManager");
            if (gameStart != null)
                gameStart.transform.SetLocalY(-2.8f);
        }
        catch { }

        if (GameState.IsFreePlay)
            __instance.Chat.gameObject.SetActive(true);
    }
}

[HarmonyPatch(typeof(KillOverlay))]
public class KillOverlayPatch
{
    [HarmonyPatch(nameof(KillOverlay.ShowKillAnimation), new Type[] { typeof(OverlayKillAnimation), typeof(NetworkedPlayerInfo), typeof(NetworkedPlayerInfo) })]
    [HarmonyPrefix]
    public static bool ShowKillAnimation_Prefix()
    {
        if (!PlayerControl.LocalPlayer.IsAlive())
        {
            return false;
        }

        return true;
    }
}

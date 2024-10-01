using HarmonyLib;
using TMPro;
using UnityEngine;


namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HudManager))]
public class HudManagerPatch
{
    public static string WelcomeMessage = $"<b><color=#00b530><size=125%><align=\"center\">{string.Format(Translator.GetString("WelcomeMsg.WelcomeToBAU"), Translator.GetString("BetterAmongUs"))}\n{Main.GetVersionText()}</size>\n" +
        $"{Translator.GetString("WelcomeMsg.ThanksForDownloading")}</align></color></b>\n<size=120%> </size>\n" +
        string.Format(Translator.GetString("WelcomeMsg.BAUDescription1"), Translator.GetString("bau"), Translator.GetString("BetterOption.AntiCheat")) + "\n\n" +
        string.Format(Translator.GetString("WelcomeMsg.BAUDescription2"), Translator.GetString("bau"), Translator.GetString("BetterOption"), Translator.GetString("BetterOption.BetterHost"));

    private static bool HasBeenWelcomed = false;

    public static GameObject ButtonsLeft;
    public static GameObject ButtonsRight;

    [HarmonyPatch(nameof(HudManager.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix(HudManager __instance)
    {
        // Set up buttons
        GameObject leftButton = UnityEngine.Object.Instantiate(__instance.UseButton.gameObject.transform.parent.gameObject, __instance.UseButton.gameObject.transform.parent.transform.parent);
        ButtonsRight = __instance.UseButton.gameObject.transform.parent.gameObject;
        ButtonsLeft = leftButton;
        leftButton.name = "BottomLeft";
        leftButton.transform.DestroyChildren();
        leftButton.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(10f, 0.7f, -9f);
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
                BAUNotification.name = "BAUNotification";
                UnityEngine.Object.Destroy(BAUNotification.GetComponent<ChatNotification>());
                UnityEngine.Object.Destroy(GameObject.Find($"{BAUNotification.name}/Sizer/PoolablePlayer"));
                UnityEngine.Object.Destroy(GameObject.Find($"{BAUNotification.name}/Sizer/ColorText"));
                BAUNotification.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-1.57f, 5.3f, -15f);
                GameObject.Find($"{BAUNotification.name}/Sizer/NameText").transform.localPosition = new Vector3(-3.3192f, -0.0105f);
                BetterNotificationManager.NameText = GameObject.Find($"{BAUNotification.name}/Sizer/NameText").GetComponent<TextMeshPro>();
                UnityEngine.Object.DontDestroyOnLoad(BAUNotification);
                BetterNotificationManager.BAUNotificationManagerObj = BAUNotification;
                BAUNotification.SetActive(false);
                ChatNotifications.timeOnScreen = 0f;
                ChatNotifications.gameObject.SetActive(false);
            }
        }

        _ = new LateTask(() =>
        {
            if (!HasBeenWelcomed && GameStates.IsInGame && GameStates.IsLobby && !GameStates.IsFreePlay)
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

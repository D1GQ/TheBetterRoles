using HarmonyLib;
using System.Text;
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
            if (!HasBeenWelcomed && GameStates.IsInGame && GameStates.IsLobby && !GameStates.IsFreePlay)
            {
                BetterNotificationManager.Notify($"<b><color=#00751f>{string.Format(Translator.GetString("WelcomeMsg.WelcomeToTBR"), Translator.GetString("TheBetterRoles"))}!</color></b>", 8f);
                HasBeenWelcomed = true;
            }
        }, 1f, "HudManagerPatch Start");
    }
    private static int AddonIndex = 0;
    [HarmonyPatch(nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(HudManager __instance)
    {
        if (Input.GetKeyDown(KeyCode.F1) && PlayerControl.LocalPlayer.Role() != null)
        {
            var role = PlayerControl.LocalPlayer.Role();
            StringBuilder sb = new();
            sb.Append($"<size=75%>{string.Format(Translator.GetString("Role"), Utils.GetCustomRoleNameAndColor(role.RoleType))}\n");
            sb.Append($"{string.Format(Translator.GetString("Role.Team"), $"<{Utils.GetCustomRoleTeamColor(role.RoleTeam)}>{Utils.GetCustomRoleTeamName(role.RoleTeam)}</color>")}\n");
            sb.Append(string.Format(Translator.GetString("Role.Category"), $"{Utils.GetCustomRoleCategoryName(role.RoleCategory)}\n\n"));
            sb.Append($"{Utils.GetCustomRoleInfo(role.RoleType, true)}</size>");
            __instance.ShowPopUp(sb.ToString());
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            var addons = PlayerControl.LocalPlayer.BetterData().RoleInfo.Addons.ToList();
            if (addons.Any())
            {
                if (AddonIndex >= addons.Count) AddonIndex = 0;
                var addon = addons[AddonIndex];
                AddonIndex++;

                if (addon != null)
                {
                    StringBuilder sb = new();
                    sb.Append($"<size=75%>{string.Format(Translator.GetString("Role.Addon"), Utils.GetCustomRoleNameAndColor(addon.RoleType))}\n");
                    sb.Append(string.Format(Translator.GetString("Role.Category"), $"{Utils.GetCustomRoleCategoryName(addon.RoleCategory)}\n\n"));
                    sb.Append($"{Utils.GetCustomRoleInfo(addon.RoleType, true)}</size>");
                    __instance.ShowPopUp(sb.ToString());
                }
            }
        }

        try
        {
            GameObject gameStart = GameObject.Find("GameStartManager");
            if (gameStart != null)
                gameStart.transform.SetLocalY(-2.8f);
        }
        catch { }

        if (GameStates.IsFreePlay)
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

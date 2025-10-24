using HarmonyLib;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network.Configs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.Manager;

[HarmonyPatch(typeof(HudManager))]
internal class HudManagerPatch
{
    internal static GameObject? ButtonsLeft;
    internal static GameObject? ButtonsRight;
    internal static Sprite? catchedUseSprite;
    internal static GameObject? VanillaButtons;

    [HarmonyPatch(nameof(HudManager.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(HudManager __instance)
    {
        var SettingsHud = new GameObject("SettingsHud");
        SettingsHud.transform.SetParent(__instance.transform);
        SettingsHud.AddComponent<SettingsHudDisplay>();

        catchedUseSprite = __instance.UseButton.graphic.sprite;
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

        if (TBRNotificationManager.TBRNotificationManagerObj == null)
        {
            var ChatNotifications = __instance.Chat.chatNotification;
            if (ChatNotifications != null)
            {
                ChatNotifications.timeOnScreen = 1f;
                ChatNotifications.gameObject.SetActive(true);
                GameObject TBRNotification = UnityEngine.Object.Instantiate(ChatNotifications.gameObject);
                TBRNotification.name = "TBRNotification";
                TBRNotification.GetComponent<ChatNotification>().DestroyMono();
                GameObject.Find($"{TBRNotification.name}/Sizer/PoolablePlayer").DestroyObj();
                GameObject.Find($"{TBRNotification.name}/Sizer/ColorText").DestroyObj();
                TBRNotification.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-1.57f, 5.3f, -15f);
                GameObject.Find($"{TBRNotification.name}/Sizer/NameText").transform.localPosition = new Vector3(-3.3192f, -0.0105f);
                TBRNotificationManager.NameText = GameObject.Find($"{TBRNotification.name}/Sizer/NameText").GetComponent<TextMeshPro>();
                UnityEngine.Object.DontDestroyOnLoad(TBRNotification);
                TBRNotificationManager.TBRNotificationManagerObj = TBRNotification;
                TBRNotification.SetActive(false);
                ChatNotifications.timeOnScreen = 0f;
                ChatNotifications.gameObject.SetActive(false);
                TBRNotificationManager.TextArea.enableWordWrapping = true;
                TBRNotificationManager.TextArea.m_firstOverflowCharacterIndex = 0;
                TBRNotificationManager.TextArea.overflowMode = TextOverflowModes.Overflow;
            }
        }

        GameObject TipTracker = new("TipTracker");
        TipTracker.transform.SetParent(HudManager.Instance.transform);
        TipTracker.AddComponent<TipTracker>();

        VanillaButtons = new GameObject("VanillaButtons");
        VanillaButtons.transform.SetParent(__instance.transform);

        __instance?.ReportButton?.transform?.SetParent(VanillaButtons.transform);
        __instance.ReportButton.transform.position = new Vector2(200f, 0f);
        __instance?.KillButton?.transform?.SetParent(VanillaButtons.transform);
        __instance.KillButton.transform.position = new Vector2(200f, 0f);
        __instance?.SabotageButton?.transform?.SetParent(VanillaButtons.transform);
        __instance.SabotageButton.transform.position = new Vector2(200f, 0f);
        __instance?.AbilityButton?.transform?.SetParent(VanillaButtons.transform);
        __instance.AbilityButton.transform.position = new Vector2(200f, 0f);
    }

    [HarmonyPatch(nameof(HudManager.Update))]
    [HarmonyPostfix]
    private static void Update_Postfix(HudManager __instance)
    {
        SettingsHudDisplay.Instance?.gameObject.SetActive(GameState.IsLobby);
        if (GameStartManager.InstanceExists)
        {
            GameStartManager.Instance.transform.SetLocalY(-2.8f);
        }

        if (GameState.IsFreePlay || (TBRGameSettings.Debugging.GetBool() && Main.MyData.IsDev()))
            __instance.Chat.gameObject.SetActive(true);
    }

    [HarmonyPatch(nameof(HudManager.ShowEmblem))]
    [HarmonyPrefix]
    private static void ShowEmblem_Prefix() => HudManager.Instance?.GameLoadAnimation?.gameObject?.SetActive(false);
}

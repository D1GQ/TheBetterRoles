using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

internal class MainMenuPatch
{
    private static List<PassiveButton> buttons = [];
    private static List<GameObject> buttonsObj = [];
    private static PassiveButton template;
    private static PassiveButton creditsButton;
    private static PassiveButton gitHubButton;
    private static PassiveButton discordButton;

    // Handle FileChecker
    [HarmonyPatch(typeof(AccountManager))]
    internal class AccountManagerPatch
    {
        [HarmonyPatch(nameof(AccountManager.CanPlayOnline))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!FileChecker.HasShownPopUp && FileChecker.CheckIfUnauthorizedFiles())
            {
                var lines = "<color=#ebbd34>----------------------------------------------------------------------------------------------</color>";
                var icon = $"<color=#278720>{Translator.GetString("BAUMark")}</color>";
                var warning = $"<color=#e60000>{Translator.GetString("WarningIcon")}</color>";
                FileChecker.HasShownPopUp = true;
                Utils.ShowPopUp($"{lines}\n<b><size=200%>{icon}<color=#0ed400>{Translator.GetString("BetterAmongUs")}</color>{icon}</size></b>\n<color=#757575><u><size=150%>{warning}<color=#8f0000>{FileChecker.UnauthorizedReason}</color>{warning}</size></u>\n\n<color=white>\n{Translator.GetString("FileChecker.OnlineMsg")}\n{lines}");
            }
        }
    }

    // Replace AU logo with BAU logo
    [HarmonyPatch(typeof(MainMenuManager))]
    internal class MainMenuManagerPatch
    {
        [HarmonyPatch(nameof(MainMenuManager.Start))]
        [HarmonyPostfix]
        public static void Postfix(MainMenuManager __instance)
        {
            if (template == null) template = __instance.quitButton;

            if (template == null) return;

            buttons.Clear();
            buttonsObj.Clear();

            string creditsTextTitle = "<size=150%><color=#0dff00><b>-=Mod Credits=-</b></color></size>\n";
            string creditsText = "<size=75%>◆ <color=#0088ff>Head Developer</color>: <b>D1GQ</b></size>";

            /*
            if (creditsButton == null)
            {
                creditsButton = CreateButton(
                    "CreditsButton",
                    new(255, 255, 255, byte.MaxValue),
                    new(200, 200, 200, byte.MaxValue),
                    () =>
                    {
                        DisconnectPopup.Instance.ShowCustom(creditsTextTitle + creditsText);
                    },
                    "Mod Credits"); //"Credits"
            }
            */

            if (gitHubButton == null)
            {
                gitHubButton = CreateButton(
                    "GitHubButton",
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL(""/*Main.Github*/),
                    "GitHub"); //"GitHub"
            }

            if (discordButton == null)
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(88, 101, 242, byte.MaxValue),
                    new(148, 161, byte.MaxValue, byte.MaxValue),
                    () => Application.OpenURL(Main.Discord),
                    "Discord"); //"Discord"
            }
        }

        [HarmonyPatch(nameof(MainMenuManager.LateUpdate))]
        [HarmonyPostfix]
        public static void LateUpdate_Postfix(MainMenuManager __instance)
        {
            bool Flag = __instance?.screenTint?.GetComponent<SpriteRenderer>() != null
                && !__instance.screenTint.GetComponent<SpriteRenderer>().enabled;

            foreach (var item in buttonsObj)
            {
                item.SetActive(Flag);
            }
        }

        public static PassiveButton CreateButton(string name, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
        {
            var button = UnityEngine.Object.Instantiate(template);
            buttons.Add(button);
            buttonsObj.Add(button.gameObject);
            button.name = name;
            button.GetComponent<AspectPosition>().DestroyMono();

            // Set button position and scale
            float baseY = -2.7882f;
            float newY = baseY + (0.38f * (buttons.Count - 1));
            button.transform.localPosition = new Vector3(-0.6118f, newY, -5f);
            button.transform.localScale = new Vector3(0.78f, 0.78f, 0.78f);

            // Set button action
            button.OnClick = new();
            button.OnClick.AddListener(action);

            // Set button text
            var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
            buttonText.DestroyTextTranslator();
            buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
            buttonText.enableWordWrapping = false;
            buttonText.text = label;

            // Set button colors
            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = normalColor;
            hoverSprite.color = hoverColor;

            // Align text
            var container = buttonText.transform.parent;
            container.GetComponent<AspectPosition>().DestroyMono();
            buttonText.GetComponent<AspectPosition>().DestroyMono();
            container.SetLocalX(0f);
            buttonText.transform.SetLocalX(0f);
            buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            // Set button collider
            var buttonCollider = button.GetComponent<BoxCollider2D>();
            if (scale.HasValue)
            {
                normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
            }

            buttonCollider.offset = new Vector2(0f, 0f);

            return button;
        }

    }
}

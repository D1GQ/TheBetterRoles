using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

// History and dark mode from: https://github.com/0xDrMoe/TownofHost-Enhanced

class ChatPatch
{
    public static List<string> ChatHistory = [];
    public static int CurrentHistorySelection = -1;

    [HarmonyPatch(typeof(ChatController))]
    public class ChatControllerPatch
    {
        [HarmonyPatch(nameof(ChatController.Toggle))]
        [HarmonyPostfix]
        public static void Toggle_Postfix(/*ChatController __instance*/)
        {
            SetChatTheme();
        }

        [HarmonyPatch(nameof(ChatController.Update))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Update_Prefix(ChatController __instance)
        {
            if (Main.ChatDarkMode.Value)
            {
                // Free chat color
                __instance.freeChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                __instance.freeChatField.textArea.compoText.Color(Color.white);
                __instance.freeChatField.textArea.outputText.color = Color.white;
            }
            else
            {
                // Free chat color
                __instance.freeChatField.background.color = new Color32(255, 255, 255, byte.MaxValue);
                __instance.freeChatField.textArea.compoText.Color(Color.black);
                __instance.freeChatField.textArea.outputText.color = Color.black;
            }

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
            {
                ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
                __instance.freeChatField.textArea.SetText("");
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) && ChatHistory.Any())
            {
                CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatHistory.Count - 1);
                __instance.freeChatField.textArea.SetText(ChatHistory[CurrentHistorySelection]);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) && ChatHistory.Any())
            {
                CurrentHistorySelection++;
                if (CurrentHistorySelection < ChatHistory.Count)
                    __instance.freeChatField.textArea.SetText(ChatHistory[CurrentHistorySelection]);
                else __instance.freeChatField.textArea.SetText("");
            }
        }

        // Add extra information to chat bubble
        [HarmonyPatch(nameof(ChatController.AddChat))]
        [HarmonyPostfix]
        public static void AddChat_Postfix(/*ChatController __instance,*/ [HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] string chatText)
        {
            ChatBubble? chatBubble = SetChatPoolTheme();
            if (chatBubble == null) return;

            var sbTag = new StringBuilder();

            // Put +++ at the end of each tag
            static StringBuilder FormatInfo(StringBuilder source)
            {
                var sb = new StringBuilder();
                if (source.Length > 0)
                {
                    string text = source.ToString();
                    string[] parts = text.Split("+++");
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(Utils.RemoveHtmlText(parts[i])))
                        {
                            sb.Append(parts[i]);
                            if (i != parts.Length - 2)
                            {
                                sb.Append(" - ");
                            }
                        }
                    }
                }

                return sb;
            }

            sbTag = FormatInfo(sbTag);

            var flag = sourcePlayer.IsLocalPlayer();

            // chatBubble.NameText.text = playerName;
            chatBubble.ColorBlindName.color = Palette.PlayerColors[sourcePlayer.Data.DefaultOutfit.ColorId];
            Logger.Log($"{sourcePlayer.Data.PlayerName} -> {chatText}", "ChatLog");
        }

        [HarmonyPatch(nameof(ChatController.AddChatNote))]
        [HarmonyPostfix]
        public static void AddChatNote_Postfix(ChatController __instance)
        {
            SetChatPoolTheme();
        }

        [HarmonyPatch(nameof(ChatController.AddChatWarning))]
        [HarmonyPostfix]
        public static void AddChatWarning_Postfix(ChatController __instance)
        {
            SetChatPoolTheme();
        }

        public static void SetChatTheme()
        {
            var chat = HudManager.Instance.Chat;

            if (Main.ChatDarkMode.Value)
            {
                // Quick chat color
                chat.quickChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                chat.quickChatField.text.color = Color.white;

                // Icons
                chat.quickChatButton.transform.Find("QuickChatIcon").GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
                chat.openKeyboardButton.transform.Find("OpenKeyboardIcon").GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            else
            {
                // Quick chat color
                chat.quickChatField.background.color = new Color32(255, 255, 255, byte.MaxValue);
                chat.quickChatField.text.color = Color.black;

                // Icons
                chat.quickChatButton.transform.Find("QuickChatIcon").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
                chat.openKeyboardButton.transform.Find("OpenKeyboardIcon").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            }

            foreach (var item in HudManager.Instance.Chat.chatBubblePool.activeChildren.ToArray().Select(c => c.GetComponent<ChatBubble>()))
            {
                SetChatPoolTheme(item);
            }
        }

        // Set chat theme
        public static ChatBubble? SetChatPoolTheme(ChatBubble? asChatBubble = null)
        {
            ChatBubble Get() => HudManager.Instance.Chat.chatBubblePool.activeChildren.ToArray()
                .Select(c => c.GetComponent<ChatBubble>())
                .Last();

            ChatBubble? chatBubble = asChatBubble ??= Get();
            if (chatBubble == null) return chatBubble;

            if (Main.ChatDarkMode.Value)
            {
                chatBubble.transform.Find("ChatText (TMP)").GetComponent<TextMeshPro>().color = new Color(1f, 1f, 1f, 1f);
                chatBubble.transform.Find("Background").GetComponent<SpriteRenderer>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

                if (chatBubble.transform.Find("PoolablePlayer/xMark") != null)
                {
                    if (chatBubble.transform.Find("PoolablePlayer/xMark").GetComponent<SpriteRenderer>().enabled == true)
                    {
                        chatBubble.transform.Find("Background").GetComponent<SpriteRenderer>().color = new Color(0.05f, 0.05f, 0.05f, 0.5f);
                    }
                }
            }
            else
            {
                chatBubble.transform.Find("ChatText (TMP)").GetComponent<TextMeshPro>().color = new Color(0f, 0f, 0f, 1f);
                chatBubble.transform.Find("Background").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);

                if (chatBubble.transform.Find("PoolablePlayer/xMark") != null)
                {
                    if (chatBubble.transform.Find("PoolablePlayer/xMark").GetComponent<SpriteRenderer>().enabled == true)
                    {
                        chatBubble.transform.Find("Background").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
                    }
                }
            }

            return chatBubble;
        }
    }

    [HarmonyPatch(typeof(FreeChatInputField))]
    class FreeChatInputFieldPatch
    {
        [HarmonyPatch(nameof(FreeChatInputField.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(FreeChatInputField __instance)
        {
            __instance.textArea.allowAllCharacters = true;
            __instance.textArea.AllowSymbols = true;
            __instance.textArea.AllowPaste = true;
            __instance.textArea.AllowEmail = true;
            __instance.textArea.characterLimit = 118;
            __instance.charCountText.text = "0/118";
        }
        [HarmonyPatch(nameof(FreeChatInputField.UpdateCharCount))]
        [HarmonyPostfix]
        public static void UpdateCharCount_Postfix(FreeChatInputField __instance)
        {
            int length = __instance.textArea.text.Length;
            __instance.charCountText.text = string.Format("{0}/118", length);
            __instance.charCountText.color = GetCharColor(length, UnityEngine.Color.white);
        }
    }

    private static UnityEngine.Color GetCharColor(int length, UnityEngine.Color color)
    {

        switch (length)
        {
            case int n when n > 117:
                if (ColorUtility.TryParseHtmlString("#ff0000", out UnityEngine.Color newColor1))
                    color = newColor1;
                break;
            case int n when n > 74:
                if (ColorUtility.TryParseHtmlString("#ffff00", out UnityEngine.Color newColor3))
                    color = newColor3;
                break;
            default:
                if (ColorUtility.TryParseHtmlString("#00f04c", out UnityEngine.Color newColor4))
                    color = newColor4;
                break;
        }

        return color;
    }
}

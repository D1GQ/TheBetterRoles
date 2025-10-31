using HarmonyLib;
using TheBetterRoles.Helpers;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.Chat;

[HarmonyPatch(typeof(ChatController))]
internal class ChatControllerPatch
{
    internal static List<string> ChatHistory = [];
    internal static int CurrentHistorySelection = -1;

    [HarmonyPatch(nameof(ChatController.Toggle))]
    [HarmonyPostfix]
    private static void Toggle_Postfix(/*ChatController __instance*/)
    {
        SetChatTheme();
    }

    [HarmonyPatch(nameof(ChatController.Update))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Update_Prefix(ChatController __instance)
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

    [HarmonyPatch(nameof(ChatController.GetPooledBubble))]
    [HarmonyPostfix]
    private static void GetPooledBubble_Postfix(ChatController __instance, ChatBubble __result)
    {
        SetChatPoolTheme(__result);
    }

    internal static void SetChatTheme()
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

        foreach (var item in HudManager.Instance.Chat.chatBubblePool.activeChildren.SelectIL2CPP(c => c.GetComponent<ChatBubble>()))
        {
            SetChatPoolTheme(item);
        }
    }

    // Set chat theme
    internal static ChatBubble SetChatPoolTheme(ChatBubble asChatBubble)
    {
        ChatBubble chatBubble = asChatBubble;

        if (Main.ChatDarkMode.Value)
        {
            chatBubble.transform.Find("ChatText (TMP)").GetComponentInChildren<TextMeshPro>(true).color = new Color(1f, 1f, 1f, 1f);
            chatBubble.transform.Find("Background").GetComponentInChildren<SpriteRenderer>(true).color = new Color(0.15f, 0.15f, 0.15f, 1f);

            if (chatBubble.transform.Find("PoolablePlayer/xMark") != null)
            {
                if (chatBubble.transform.Find("PoolablePlayer/xMark").GetComponentInChildren<SpriteRenderer>(true).enabled == true)
                {
                    chatBubble.transform.Find("Background").GetComponentInChildren<SpriteRenderer>(true).color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                }
            }
        }
        else
        {
            chatBubble.transform.Find("ChatText (TMP)").GetComponentInChildren<TextMeshPro>(true).color = new Color(0f, 0f, 0f, 1f);
            chatBubble.transform.Find("Background").GetComponentInChildren<SpriteRenderer>(true).color = new Color(1f, 1f, 1f, 1f);

            if (chatBubble.transform.Find("PoolablePlayer/xMark") != null)
            {
                if (chatBubble.transform.Find("PoolablePlayer/xMark").GetComponentInChildren<SpriteRenderer>(true).enabled == true)
                {
                    chatBubble.transform.Find("Background").GetComponentInChildren<SpriteRenderer>(true).color = new Color(1f, 1f, 1f, 0.5f);
                }
            }
        }

        return chatBubble;
    }
}
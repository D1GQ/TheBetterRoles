using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.Chat;

[HarmonyPatch(typeof(FreeChatInputField))]
internal class FreeChatInputFieldPatch
{
    [HarmonyPatch(nameof(FreeChatInputField.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix(FreeChatInputField __instance)
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
    private static void UpdateCharCount_Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.text = string.Format("{0}/118", length);
        __instance.charCountText.color = GetCharColor(length, Color.white);
    }

    private static Color GetCharColor(int length, Color color)
    {

        switch (length)
        {
            case int n when n > 117:
                if (ColorUtility.TryParseHtmlString("#ff0000", out Color newColor1))
                    color = newColor1;
                break;
            case int n when n > 74:
                if (ColorUtility.TryParseHtmlString("#ffff00", out Color newColor3))
                    color = newColor3;
                break;
            default:
                if (ColorUtility.TryParseHtmlString("#00f04c", out Color newColor4))
                    color = newColor4;
                break;
        }

        return color;
    }
}

using HarmonyLib;
using TheBetterRoles.Commands;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ChatController))]
class CommandsPatch
{
    private static bool _enabled = true;
    public static string CommandPrefix => Main.CommandPrefix.Value;

    // Run code for specific commands
    private static void HandleCommand()
    {
        if (closestCommand != null)
        {
            string[] typedParts = typedCommand.Split(' ');

            for (int i = 1; i < typedParts.Length && i < closestCommand.Arguments.Length + 1; i++)
            {
                if (closestCommand.Arguments[i - 1] != null)
                {
                    closestCommand.Arguments[i - 1].Arg = typedParts[i];
                }
            }

            closestCommand.Run();
        }
        else
        {
            Utils.AddChatPrivate("<color=#f50000><size=150%><b>Invalid Command!</b></size></color>");
        }
    }

    // Check if command is typed when sending chat message
    [HarmonyPatch(nameof(ChatController.SendChat))]
    [HarmonyPrefix]
    public static bool SendChat_Prefix(ChatController __instance)
    {
        if (!_enabled)
        {
            return true;
        }

        string text = __instance.freeChatField.textArea.text;

        if (string.IsNullOrEmpty(text) || text.Length <= 1 || text[0].ToString() != CommandPrefix || 3f - __instance.timeSinceLastMessage > 0f)
        {
            if (GameState.InGame && !GameState.IsLobby && !GameState.IsFreePlay && !GameState.IsMeeting && !GameState.IsExilling && PlayerControl.LocalPlayer.IsAlive())
                return false;

            if (ChatPatch.ChatHistory.Count == 0 || ChatPatch.ChatHistory[^1] != text) ChatPatch.ChatHistory.Add(text);
            ChatPatch.CurrentHistorySelection = ChatPatch.ChatHistory.Count;
            return true;
        }

        HandleCommand();

        if (ChatPatch.ChatHistory.Count == 0 || ChatPatch.ChatHistory[^1] != text) ChatPatch.ChatHistory.Add(text);
        ChatPatch.CurrentHistorySelection = ChatPatch.ChatHistory.Count;

        __instance.timeSinceLastMessage = 0f;
        __instance.freeChatField.Clear();
        __instance.quickChatMenu.Clear();
        __instance.quickChatField.Clear();
        return false;
    }

    // Set up command helper
    private static GameObject commandText;
    private static GameObject commandInfo;
    private static RandomNameGenerator NameRNG;
    [HarmonyPatch(nameof(ChatController.Toggle))]
    [HarmonyPostfix]
    public static void Awake_Postfix(ChatController __instance)
    {
        if (commandText == null)
        {
            var TextArea = __instance.freeChatField.textArea.gameObject;
            GameObject CommandDisplay = UnityEngine.Object.Instantiate(TextArea, TextArea.transform.parent.transform);
            CommandDisplay.transform.SetSiblingIndex(TextArea.transform.GetSiblingIndex() + 1);
            CommandDisplay.transform.DestroyChildren();
            CommandDisplay.name = "CommandArea";
            CommandDisplay.GetComponent<TextMeshPro>().color = new Color(1f, 1f, 1f, 0.5f);
            commandText = CommandDisplay;
        }

        if (commandInfo == null)
        {
            var TextArea = __instance.freeChatField.textArea.gameObject;
            GameObject CommandInformation = UnityEngine.Object.Instantiate(TextArea, TextArea.transform.parent.transform);
            CommandInformation.transform.SetSiblingIndex(TextArea.transform.GetSiblingIndex() + 1);
            CommandInformation.transform.DestroyChildren();
            CommandInformation.transform.localPosition = new Vector3(CommandInformation.transform.localPosition.x, 0.45f);
            CommandInformation.name = "CommandInfoText";
            CommandInformation.GetComponent<TextMeshPro>().color = Color.yellow;
            CommandInformation.GetComponent<TextMeshPro>().outlineColor = new Color(0f, 0f, 0f, 1f);
            CommandInformation.GetComponent<TextMeshPro>().outlineWidth = 0.2f;
            CommandInformation.GetComponent<TextMeshPro>().characterWidthAdjustment = 1.5f;
            CommandInformation.GetComponent<TextMeshPro>().enableWordWrapping = false;
            commandInfo = CommandInformation;
        }

        if (NameRNG == null)
        {
            RandomNameGenerator rng = __instance.gameObject.AddComponent<RandomNameGenerator>();
            NameRNG = rng;
        }
    }

    private static string typedCommand;
    private static BaseCommand? closestCommand;

    // Command helper
    [HarmonyPatch(nameof(ChatController.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(ChatController __instance)
    {
        if (!_enabled)
        {
            commandText.GetComponent<TextMeshPro>().text = string.Empty;
            commandInfo.GetComponent<TextMeshPro>().text = string.Empty;
            return;
        }

        string text = __instance.freeChatField.textArea.text;

        if (commandText != null && commandInfo != null)
        {
            if (text.Length > 0)
            {
                typedCommand = text.Length > 1 ? text[1..] : string.Empty;
                string[] typedParts = typedCommand.Split(' ');

                closestCommand = GetClosestCommand(typedCommand.Split(' ')[0]);
                if (closestCommand != null && (typedParts[0].ToLower() == closestCommand.Name.ToLower() || typedParts.Length == 1))
                {
                    string CommandInfo = closestCommand.Description;

                    string suggestion = string.Empty;
                    if (typedParts.Length == 1)
                    {
                        suggestion = closestCommand.Name;
                    }
                    else if (typedParts.Last().Length < 1)
                    {
                        int nextArgumentIndex = typedParts.Length - 2;

                        if (nextArgumentIndex >= 0 && closestCommand.Arguments.Length > nextArgumentIndex)
                        {
                            suggestion = text + closestCommand.Arguments[nextArgumentIndex]?.Suggestion ?? string.Empty;
                        }
                        else
                        {
                            suggestion = closestCommand.Name;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.Tab) && typedParts.Length == 1)
                    {
                        __instance.freeChatField.textArea.SetText(CommandPrefix + closestCommand.Name);
                    }

                    string fullSuggestion = CommandPrefix + suggestion;
                    if (text.Length <= fullSuggestion.Length)
                    {
                        commandText.GetComponent<TextMeshPro>().text = text + fullSuggestion[text.Length..];
                    }
                    else
                    {
                        commandText.GetComponent<TextMeshPro>().text = string.Empty;
                    }

                    commandInfo.GetComponent<TextMeshPro>().text = CommandInfo;
                }
                else
                {
                    // Clear the suggestion if there is a mismatch
                    commandText.GetComponent<TextMeshPro>().text = string.Empty;
                    commandInfo.GetComponent<TextMeshPro>().text = string.Empty;
                }
            }
            else
            {
                commandText.GetComponent<TextMeshPro>().text = string.Empty;
                commandInfo.GetComponent<TextMeshPro>().text = string.Empty;
            }
        }
    }

    public static BaseCommand? GetClosestCommand(string typedCommand)
    {
        var closestCommand = BaseCommand.allCommands.FirstOrDefault(c => c.Name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase) && c.Type == CommandType.Normal);
        closestCommand ??= BaseCommand.allCommands.FirstOrDefault(c => c.Name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase) && c.Type == CommandType.Sponsor);

#if DEBUG || DEBUG_MULTIACCOUNTS
        if (GameState.IsDev)
        {
            closestCommand ??= BaseCommand.allCommands.FirstOrDefault(c => c.Name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase) && c.Type == CommandType.Debug);
        }
#endif

        return closestCommand;
    }
}

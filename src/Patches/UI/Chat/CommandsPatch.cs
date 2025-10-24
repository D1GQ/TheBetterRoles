using HarmonyLib;
using TheBetterRoles.Commands;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.Chat;

[HarmonyPatch(typeof(ChatController))]
internal class CommandsPatch
{
    private static bool _enabled = true;
    internal static string CommandPrefix => Main.CommandPrefix.Value;

    // Run code for specific commands
    private static void HandleCommand()
    {
        if (closestCommand != null && isTypedOut)
        {
            closestCommand.Run();
        }
        else
        {
            Utils.AddChatPrivate("<color=#f50000><size=150%><b>Invalid Command!</b></size></color>");
        }
    }

    private static void ClearCommandDisplay()
    {
        isTypedOut = false;
        commandText.text = string.Empty;
        commandInfo.text = string.Empty;
    }

    private static void HandleValidSuggestion(ChatController __instance, string[] typedParts)
    {
        isTypedOut = true;

        string suggestion = GenerateSuggestion(typedParts);
        string fullSuggestion = CommandPrefix + suggestion;

        if (Input.GetKeyDown(KeyCode.Tab) && typedParts.Length >= 1)
        {
            __instance.freeChatField.textArea.SetText(fullSuggestion);
        }

        commandText.text = fullSuggestion;
        commandInfo.text = $"{closestCommand.Description}{GenerateArgumentInfo()}";
    }

    private static string GenerateSuggestion(string[] typedParts)
    {
        if (closestCommand == null || typedParts.Length == 0)
            return string.Empty;

        // Handle base command name suggestion
        if (typedParts.Length == 1)
        {
            // Preserve the user's casing for the portion they've typed
            string userTyped = typedParts[0];
            string commandName = closestCommand.Name;

            // If user typed nothing or we can't preserve casing, return full command name
            if (userTyped.Length == 0 || userTyped.Length > commandName.Length)
                return commandName;

            // Preserve user's casing for the portion they typed, use command's casing for the rest
            return userTyped + commandName.Substring(userTyped.Length);
        }

        UpdateCommandArguments(typedParts);

        int nextArgumentIndex = typedParts.Length - 2;
        if (nextArgumentIndex >= 0 && nextArgumentIndex < closestCommand.Arguments.Length)
        {
            var argument = closestCommand.Arguments[nextArgumentIndex];
            var closestSuggestion = argument.GetClosestSuggestion();
            string currentArg = typedParts[typedParts.Length - 1];

            if (string.IsNullOrEmpty(closestSuggestion))
                return string.Empty;

            // Preserve user's casing for the portion they typed
            if (currentArg.Length > 0 && currentArg.Length <= closestSuggestion.Length)
            {
                return typedCommand + closestSuggestion.Substring(currentArg.Length);
            }

            return typedCommand + closestSuggestion;
        }

        return string.Empty;
    }

    private static void UpdateCommandArguments(string[] typedParts)
    {
        for (int i = 1; i < typedParts.Length && i <= closestCommand.Arguments.Length; i++)
        {
            if (closestCommand.Arguments[i - 1] != null)
            {
                closestCommand.Arguments[i - 1].Arg = typedParts[i];
            }
        }
    }

    private static string GenerateArgumentInfo()
    {
        if (closestCommand.Arguments.Length == 0)
            return string.Empty;

        var argumentInfo = string.Join(" ", closestCommand.Arguments.Select(arg => arg.GetArgInfo()));
        return $" - <#a6a6a6>{argumentInfo}</color>";
    }


    internal static BaseCommand? GetClosestCommand(string typedCommand)
    {
        var directNormalMatch = BaseCommand.allCommands
            .FirstOrDefault(c => c.Type == CommandType.Normal
                                 && c.Names.Any(name => string.Equals(name, typedCommand, StringComparison.OrdinalIgnoreCase))
                                 && c.ShowCommand());
        if (directNormalMatch != null)
            return directNormalMatch;

        var closestNormalCommand = BaseCommand.allCommands
            .OrderBy(c => c.Name)
            .FirstOrDefault(c => c.Type == CommandType.Normal
                                 && c.Names.Any(name => name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase))
                                 && c.ShowCommand());
        if (closestNormalCommand != null)
            return closestNormalCommand;

        if (Main.MyData.IsSponsor())
        {
            var directSponsorMatch = BaseCommand.allCommands
                .FirstOrDefault(c => c.Type == CommandType.Sponsor
                         && c.Names.Any(name => string.Equals(name, typedCommand, StringComparison.OrdinalIgnoreCase))
                         && c.ShowCommand());
            if (directSponsorMatch != null)
                return directSponsorMatch;

            var closestSponsorCommand = BaseCommand.allCommands
                .OrderBy(c => c.Name)
                .FirstOrDefault(c => c.Type == CommandType.Sponsor
                                 && c.Names.Any(name => name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase))
                                 && c.ShowCommand());
            if (closestSponsorCommand != null)
                return closestSponsorCommand;
        }

        if (Main.MyData.IsDev())
        {
            var directdebugCommand = BaseCommand.allCommands
                .FirstOrDefault(c => c.Type == CommandType.Debug
                                     && c.Names.Any(name => string.Equals(name, typedCommand, StringComparison.OrdinalIgnoreCase))
                                     && c.ShowCommand());
            if (directdebugCommand != null)
                return directdebugCommand;

            var closestDebugCommand = BaseCommand.allCommands
                .OrderBy(c => c.Name)
                .FirstOrDefault(c => c.Type == CommandType.Debug
                     && c.Names.Any(name => name.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase))
                     && c.ShowCommand());
            if (closestDebugCommand != null)
                return closestDebugCommand;
        }

        return null;
    }

    // Check if command is typed when sending chat message
    [HarmonyPatch(nameof(ChatController.SendChat))]
    [HarmonyPrefix]
    private static bool SendChat_Prefix(ChatController __instance)
    {
        if (!_enabled)
        {
            return true;
        }

        string text = __instance.freeChatField.textArea.text;

        if (!text.StartsWith(CommandPrefix) || 3f - __instance.timeSinceLastMessage > 0f)
        {
            if (GameState.IsInGame && !GameState.IsLobby && !GameState.IsFreePlay && !GameState.IsMeeting && !GameState.IsExilling && PlayerControl.LocalPlayer.IsAlive())
                return false;

            if (ChatControllerPatch.ChatHistory.Count == 0 || ChatControllerPatch.ChatHistory[^1] != text) ChatControllerPatch.ChatHistory.Add(text);
            ChatControllerPatch.CurrentHistorySelection = ChatControllerPatch.ChatHistory.Count;
            return true;
        }

        HandleCommand();

        if (ChatControllerPatch.ChatHistory.Count == 0 || ChatControllerPatch.ChatHistory[^1] != text) ChatControllerPatch.ChatHistory.Add(text);
        ChatControllerPatch.CurrentHistorySelection = ChatControllerPatch.ChatHistory.Count;

        if (closestCommand?.SetChatTimer == true)
        {
            __instance.timeSinceLastMessage = 0f;
        }

        __instance.freeChatField.Clear();
        __instance.quickChatMenu.Clear();
        __instance.quickChatField.Clear();

        return false;
    }

    // Set up command helper
    private static TextMeshPro commandText;
    private static TextMeshPro commandInfo;
    [HarmonyPatch(nameof(ChatController.Toggle))]
    [HarmonyPostfix]
    private static void Awake_Postfix(ChatController __instance)
    {
        if (commandText == null)
        {
            var TextArea = __instance.freeChatField.textArea.outputText;
            commandText = UnityEngine.Object.Instantiate(TextArea, TextArea.transform.parent.transform);
            commandText.transform.SetSiblingIndex(TextArea.transform.GetSiblingIndex() + 1);
            commandText.transform.DestroyChildren();
            commandText.name = "CommandArea";
            commandText.GetComponent<TextMeshPro>().color = new Color(1f, 1f, 1f, 0.5f);
        }

        if (commandInfo == null)
        {
            var TextArea = __instance.freeChatField.textArea.outputText;
            commandInfo = UnityEngine.Object.Instantiate(TextArea, TextArea.transform.parent.transform);
            commandInfo.transform.SetSiblingIndex(TextArea.transform.GetSiblingIndex() + 1);
            commandInfo.transform.DestroyChildren();
            commandInfo.transform.localPosition = new Vector3(commandInfo.transform.localPosition.x, 0.45f);
            commandInfo.name = "CommandInfoText";
            commandInfo.GetComponent<TextMeshPro>().color = Color.yellow;
            commandInfo.GetComponent<TextMeshPro>().outlineColor = new Color(0f, 0f, 0f, 1f);
            commandInfo.GetComponent<TextMeshPro>().outlineWidth = 0.2f;
            commandInfo.GetComponent<TextMeshPro>().characterWidthAdjustment = 1.5f;
            commandInfo.GetComponent<TextMeshPro>().enableWordWrapping = false;
        }
    }

    private static bool isTypedOut;
    private static string typedCommand;
    private static BaseCommand? closestCommand;

    // Command helper
    [HarmonyPatch(nameof(ChatController.Update))]
    [HarmonyPostfix]
    private static void Update_Postfix(ChatController __instance)
    {
        if (!_enabled)
        {
            ClearCommandDisplay();
            return;
        }

        string text = __instance.freeChatField.textArea.text;

        if (commandText == null || commandInfo == null)
            return;

        if (text.Length > 0 && text.StartsWith(CommandPrefix))
        {
            typedCommand = text.Length > 1 ? text[1..] : string.Empty;
            string[] typedParts = typedCommand.Split(' ');

            closestCommand = GetClosestCommand(typedParts[0]);
            bool isSuggestionValid = closestCommand != null
                && (typedParts[0].Equals(closestCommand.Name, StringComparison.OrdinalIgnoreCase) || typedParts.Length == 1)
                && closestCommand.ShowSuggestion();

            if (isSuggestionValid)
            {
                HandleValidSuggestion(__instance, typedParts);
            }
            else
            {
                ClearCommandDisplay();
            }
        }
        else
        {
            ClearCommandDisplay();
        }
    }
}

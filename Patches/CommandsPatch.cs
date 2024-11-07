using AmongUs.Data;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ChatController))]
class CommandsPatch
{
    private static bool _enabled = true;
    private static PlayerControl? cmdTarget = null;
    private static bool HasPermission => Permission != null
        && AmongUsClient.Instance?.GetHost()?.Character?.Data == Permission;
    public static NetworkedPlayerInfo? Permission = null;
    public static string CommandPrefix => Main.CommandPrefix.Value;

    // List of helper text when a command is being typed out
    // First word is command, {} are arguments, first --- is command description, second --- is for help command.
    public static string[] CommandListHelper() => new string[]
    {
        "test---",
        FormatCommandTranslation("Command.Helper.help", false),
        FormatCommandTranslation("Command.Helper.commands", false),
        FormatCommandTranslation("Command.Helper.dump", false),
        FormatCommandTranslation("Command.Helper.player", false),
        FormatCommandTranslation("Command.Helper.players", false),
        FormatCommandTranslation("Command.Helper.whisper", false),
        FormatCommandTranslation("Command.Helper.whisper.trim", false),
        FormatCommandTranslation("Command.Helper.setprefix", false),
        FormatCommandTranslation("Command.Helper.name", false),
        FormatCommandTranslation("Command.Helper.kick", false),
        FormatCommandTranslation("Command.Helper.ban", false),
        FormatCommandTranslation("Command.Helper.endgame", false),
    };
    public static string[] DebugCommandListHelper() => new string[]
    {
#if DEBUG || DEBUG_MULTIACCOUNTS
        FormatCommandTranslation("Command.DebugHelper.getposition", false),
        FormatCommandTranslation("Command.DebugHelper.role", false),
        FormatCommandTranslation("Command.DebugHelper.setrole", false),
        FormatCommandTranslation("Command.DebugHelper.suicide", false),
        FormatCommandTranslation("Command.DebugHelper.exile", false),
        FormatCommandTranslation("Command.DebugHelper.revive", false),
#endif
    };

    private static string FormatCommandTranslation(string str, bool checkCommand = true)
    {
        if (checkCommand)
        {
            try
            {
                return Translator.GetString(str, showInvalid: false).Split(" ")[0].Split("---")[0].ToLower();
            }
            catch
            {
                return str;
            }
        }
        else
        {
            return string.Format(Translator.GetString(str, showInvalid: false), "", Translator.GetString("Command.HostOnly"), Translator.GetString("Command.PermOnly"), Translator.GetString("Command.DevOnly"));
        }
    }

    // Run code for specific commands
    private static void HandleCommand(ChatController __instance, string[] command)
    {
        bool checkSponsorCommand = false;
        bool checkDebugCommand = false;
        string subArgs = command.Length > 1 ? command[1].ToLower().Trim() : "";
        string subArgs2 = command.Length > 2 ? command[2].ToLower().Trim() : "";
        bool flag = subArgs.Length > 0;
        bool flag2 = subArgs2.Length > 0;
        string error = "<color=#f50000><size=150%><b>Error:</b></size></color>";
        StringBuilder sb = new StringBuilder();

        // Commands
        switch (command[0][1..].ToLower().Trim())
        {
            case "test":
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.help"):
                Utils.AddChatPrivate(
                    "Welcome to <color=#0dff00>♻BetterAmongUs♻</color> This mod enhances your gameplay experience with a variety of exciting features.\n" +
                    "Explore the pause menu to access more options and better settings tailored to your needs.\n" +
                    "For a full list of available commands, use the `/commands` command.\n\n" +
                    "Our features include: \n" +
                    "- Built-in Client-Sided Anti-Cheat: Enjoy a fair game with our anti-cheat system that detects and prevents unauthorized actions.\n" +
                    "- Host Enhancements: Gain additional control as a host with improved options and settings.\n" +
                    "- Better Options: Customize your game with a range of new and improved settings.\n" +
                    "- Commands: Utilize a variety of commands to manage and enhance your gameplay.\n" +
                    "- Client Improvements: Experience smoother and more efficient gameplay with our client-side enhancements.\n" +
                    "Stay tuned for more exciting features and improvements coming your way!"
                );
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.commands"):
                string[] allCommands = CommandListHelper();
                string[] allDebugCommands = DebugCommandListHelper();
                string list;
                var open = "<color=#858585>┌──────── </color>";
                var mid = "<color=#858585>├ </color>";
                var close = "<color=#858585>└──────── </color>";
                list = "<color=#00751f><b><size=150%>Command List</size></b></color>\n" + open;
                for (int i = 0; i < allCommands.Length; i++)
                {
                    if (i < allCommands.Length)
                    {
                        list += $"\n{mid}<color=#e0b700><b>{CommandPrefix}{allCommands[i].Split(' ')[0].Split("---")[0]}</b></color> <size=65%><color=#735e00>{allCommands[i].Split("---")[1]}.</color></size>";
                    }
                }
                if (GameState.IsDev && Main.ReleaseBuildType == ReleaseTypes.Dev)
                {
                    list += "\n" + close + "\n";
                    list += "<color=#00751f><b><size=150%>Debug Command List</size></b></color>\n" + open;
                    for (int i = 0; i < allDebugCommands.Length; i++)
                    {
                        if (i < allDebugCommands.Length)
                        {
                            list += $"\n{mid}<color=#e0b700><b>{CommandPrefix}{allDebugCommands[i].Split(' ')[0].Split("---")[0]}</b></color> <size=65%><color=#735e00>{allDebugCommands[i].Split("---")[1]}.</color></size>";
                        }
                    }
                }
                list += "\n" + close;
                Utils.AddChatPrivate(list);
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.dump"):
                if (GameState.IsInGamePlay && !GameState.IsDev) return;

                string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
                string log = File.ReadAllText(logFilePath);
                string newLog = string.Empty;
                string[] logArray = log.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string text in logArray)
                {
                    if (text.Contains("[PrivateLog]"))
                    {
                        newLog += text.Split(':')[0] + ":" + text.Split(':')[1].Replace("[PrivateLog]", "") + ": " + Encryptor.Decrypt(text.Split(':')[2][1..]) + "\n";
                    }
                    else
                    {
                        newLog += text + "\n";
                    }
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string logFolderPath = Path.Combine(desktopPath, "TheBetterRolesLog");
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }
                string logFileName = "log-" + Main.GetVersionText().Replace(' ', '-').ToLower() + "-" + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".log";
                string newLogFilePath = Path.Combine(logFolderPath, logFileName);
                File.WriteAllText(newLogFilePath, newLog);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = logFolderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = newLogFilePath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                Utils.AddChatPrivate($"Dump logs at <color=#b1b1b1>'{newLogFilePath}'</color>");
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.player"):
                if (HandlePlayerArgument(command, subArgs) == true)
                {
                    var player = cmdTarget;
                    var hexColor = Utils.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
                    var format1 = "┌ •";
                    var format2 = "├ •";
                    var format3 = "└ •";
                    sb.Append($"<size=150%><color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b></size>\n");
                    sb.Append($"{format1} <color=#c1c1c1>ID: {player?.Data?.PlayerId}</color>\n");
                    sb.Append($"{format2} <color=#c1c1c1>HashPUID: {Utils.GetHashPuid($"{player?.Data?.Puid}")}</color>\n");
                    sb.Append($"{format2} <color=#c1c1c1>Platform: {Utils.GetPlatformName(player)}</color>\n");
                    sb.Append($"{format3} <color=#c1c1c1>FriendCode: {player?.Data?.FriendCode}</color>");
                    Utils.AddChatPrivate(sb.ToString());
                }
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.players"):
                foreach (PlayerControl player in Main.AllPlayerControls.Where(player => !player.isDummy))
                {
                    var hexColor = Utils.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
                    sb.Append($"<color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b>\n");
                    sb.Append($"<color=#c1c1c1>{player?.Data?.PlayerId}</color> - ");
                    sb.Append($"<color=#c1c1c1>{Utils.GetHashPuid($"{player?.Data?.Puid}")}</color> - ");
                    sb.Append($"<color=#c1c1c1>{Utils.GetPlatformName(player)}</color> - ");
                    sb.Append($"<color=#c1c1c1>{player?.Data?.FriendCode}</color>");
                    sb.Append("\n\n");
                }
                Utils.AddChatPrivate(sb.ToString());
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.whisper") || cmd == FormatCommandTranslation("Command.Helper.whisper.trim"):
                if (HandlePlayerArgument(command, subArgs) == true)
                {
                    if (UseCommandInLobby() == true)
                    {
                        var player = cmdTarget;
                        var msg = string.Join(" ", command[2..].ToArray());

                        if (!string.IsNullOrEmpty(msg))
                        {
                            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.None, player.GetClientId());
                            messageWriter.Write(msg);
                            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                            Utils.AddChatPrivate($"{string.Join(" ", command[2..].ToArray())}", overrideName: $"<color=#696969>Sent Private Message to</color> <b>{player.GetPlayerNameAndColor()}</b>");
                        }
                        else
                        {
                            Utils.AddChatPrivate($"{error}\nUnable to send empty message");
                        }
                    }
                }
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.setprefix"):
                Main.CommandPrefix.Value = subArgs;
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.name"):
                bool hasSet = false;

                if (GameState.IsHost)
                {
                    PlayerControl.LocalPlayer.RpcSetName(command[1]);
                }
                else if (command.Length > 1 && !string.IsNullOrWhiteSpace(command[1]) && System.Text.RegularExpressions.Regex.IsMatch(command[1], @"^[a-zA-Z0-9]+$"))
                {
                    if (HandleHasPermission(command) == true)
                    {
                        PlayerControl.LocalPlayer.CmdCheckName(command[1]);
                        hasSet = true;
                    }
                }
                else
                {
                    Utils.AddChatPrivate($"{error}\nInvalid Name!");
                    return;
                }

                if (hasSet)
                {
                    PlayerControl.LocalPlayer.Data.PlayerName = command[1];
                    DataManager.Player.customization.Name = command[1];
                    Utils.AddChatPrivate($"Player name has been sent to: {command[1]}");
                }
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.kick") || cmd == FormatCommandTranslation("Command.Helper.ban"):
                if (HandlePlayerArgument(command, subArgs) == true)
                {
                    if (HandleIsHost(command) == true)
                    {
                        var player = cmdTarget;
                        var cmdFlag = command[0][1..].ToLower().Trim() != FormatCommandTranslation("Command.Helper.kick");
                        player.Kick(cmdFlag);
                    }
                }
                break;
            case var cmd when cmd == FormatCommandTranslation("Command.Helper.endgame"):
                if (HandleIsHost(command) == true)
                {
                    if (!GameState.IsLobby && !GameState.IsFreePlay)
                    {
                        if (subArgs == "")
                        {
                            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                        }
                        else if (subArgs is "impostor" or "1")
                        {
                            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                        }
                        else if (subArgs is "crewmate" or "2")
                        {
                            GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                        }
                    }
                }
                break;
            default:
                if (GameState.IsDev && Main.ReleaseBuildType == ReleaseTypes.Dev)
                {
                    checkDebugCommand = true;
                    break;
                }

                Utils.AddChatPrivate("<color=#f50000><size=150%><b>Invalid Command!</b></size></color>");
                break;
        }

#if DEBUG || DEBUG_MULTIACCOUNTS
        // DeBug Commands
        if (checkDebugCommand)
        {
            switch (command[0][1..].ToLower().Trim())
            {
                case var cmd when cmd == FormatCommandTranslation("Command.DebugHelper.getposition"):
                    Utils.AddChatPrivate($"Current position: {PlayerControl.LocalPlayer.GetTruePosition()}");
                    break;
                case var cmd when cmd == FormatCommandTranslation("Command.DebugHelper.role") || cmd == FormatCommandTranslation("Command.DebugHelper.setrole"):
                    if (HandleIsHost(command) == true)
                    {
                        if (command[0][1..].ToLower().Trim() == FormatCommandTranslation("Command.DebugHelper.role"))
                        {
                        }
                        else if (HandlePlayerArgument(command, subArgs) == true)
                        {
                        }
                    }
                    break;
                case var cmd when cmd == FormatCommandTranslation("Command.DebugHelper.suicide"):
                    if (HandleIsHost(command) == true)
                    {
                        PlayerControl.LocalPlayer.MurderPlayer(PlayerControl.LocalPlayer, MurderResultFlags.Succeeded);
                    }
                    break;
                case var cmd when cmd == FormatCommandTranslation("Command.DebugHelper.exile"):
                    if (HandleIsHost(command) == true)
                    {
                        PlayerControl.LocalPlayer.Exiled();
                    }
                    break;
                case var cmd when cmd == FormatCommandTranslation("Command.DebugHelper.revive"):
                    if (HandleIsHost(command) == true)
                    {
                        PlayerControl.LocalPlayer.Revive();
                    }
                    break;
                default:
                    Utils.AddChatPrivate("<color=#f50000><size=150%><b>Invalid Command!</b></size></color>");
                    break;
            }
        }
#endif

        cmdTarget = null;
    }

    // Condition when a player is allowed to run a specific command
    private static bool UseCommandInGame()
    {
        bool flag = GameState.IsInGame && GameState.IsMeeting || GameState.IsExilling || GameState.IsLobby || GameState.IsFreePlay || !PlayerControl.LocalPlayer.IsAlive();
        if (!flag)
        {
            Utils.AddChatPrivate("<color=#f50000><size=125%><b>Unable To Use Command While In Game!</b></size></color>");
        }
        return flag;
    }

    private static bool UseCommandInLobby()
    {
        bool flag = GameState.IsLobby || GameState.IsFreePlay;
        if (!flag)
        {
            Utils.AddChatPrivate("<color=#f50000><size=125%><b>Unable To Use Command While In Game!</b></size></color>");
        }
        return flag;
    }

    // Handle player ID arguments in commands
    private static bool HandlePlayerArgument(string[] command, string subArg)
    {
        string error = "<color=#f50000><size=150%><b>Error:</b></size></color>";

        bool flag = subArg.Where(char.IsDigit).ToArray().Any();
        bool flag2 = command.Length > 2;
        bool flag3 = false;

        if (flag)
            flag3 = Main.AllPlayerControls.ToArray().Any(player => !player.isDummy && player.Data.PlayerId == int.Parse(subArg.Where(char.IsDigit).ToArray()));

        if (flag3 == true && int.TryParse(subArg, out var num))
        {
            cmdTarget = Utils.PlayerFromPlayerId(num);
            return true;
        }
        else
        {
            if (!flag)
            {
                if (!flag2)
                {
                    Utils.AddChatPrivate($"<color=#e0b700>{error}\n{command[0][1..]} " + "<b>{Player ID}</b></color>");
                }
                else
                {
                    Utils.AddChatPrivate($"<color=#e0b700>{error}\n{command[0][1..]} {command[1]} " + "<b>{Player ID}</b></color>");
                }
            }
            else
            {
                Utils.AddChatPrivate($"<color=#730000>{error}\nPlayer not found");
            }

            return false;
        }
    }

    // Handle host only commands
    private static bool HandleIsHost(string[] command)
    {
        string error = "<color=#f50000><size=150%><b>Error:</b></size></color>";

        bool flag = PlayerControl.LocalPlayer.IsHost();

        if (!flag)
        {
            Utils.AddChatPrivate($"{error}\n<color=#e0b700><b>{command[0]}</b></color> Is only available as host!");
        }

        return flag;
    }

    // Handle if player has permission from the host to run command
    private static bool HandleHasPermission(string[] command)
    {
        string error = "<color=#f50000><size=150%><b>Error:</b></size></color>";

        if (HasPermission != true || !GameState.IsHost && !GameState.IsDev)
        {
            Utils.AddChatPrivate($"{error}\n<color=#e0b700><b>{command[0]}</b></color> Is only available with the host permission!\nask the host to type /allow in chat to get permissions");
            return false;
        }
        return true;
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

        string[] command = text.Split(' ');

        HandleCommand(__instance, command);

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
            // Check if the first character is the command prefix
            if (text.Length > 0 && text[0].ToString() == CommandPrefix)
            {
                // Get the typed command without the prefix
                string typedCommand = text.Substring(1);

                // Find the closest matching command
                string closestCommand = GetClosestCommand(typedCommand.Split(' ')[0]);
                string CommandInfo = GetClosestCommand(typedCommand.Split(' ')[0]);

                // Check for character mismatches
                if (!string.IsNullOrEmpty(closestCommand) && !string.IsNullOrEmpty(CommandInfo) && IsMatch(typedCommand.Split(' ')[0], closestCommand))
                {
                    closestCommand = closestCommand.Split("---")[0];
                    CommandInfo = CommandInfo.Split("---")[1];

                    // Handle arguments correctly
                    string[] typedParts = typedCommand.Split(' ');
                    string[] commandParts = closestCommand.Split(' ');

                    // If there are arguments, ensure to display the appropriate part
                    string suggestion = commandParts[0].Substring(typedParts[0].Length); // Initial suggestion

                    suggestion = typedParts[0].Substring(0, typedParts[0].Length) + suggestion;

                    // Handle additional arguments
                    for (int i = 1; i < typedParts.Length; i++)
                    {
                        if (i < commandParts.Length && typedParts[i] != "")
                        {
                            // Add spaces to match the already typed part of the current argument
                            suggestion = typedCommand + " " + new string(' ', typedParts[i].Length);
                        }
                        else if (i < commandParts.Length)
                        {
                            // Add the next argument if the current one is empty
                            suggestion = typedCommand + " " + commandParts[i];
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.Tab) && typedParts.Length < 2)
                    {
                        __instance.freeChatField.textArea.SetText(CommandPrefix + commandParts[0]);
                    }

                    // Display the command suggestion
                    commandText.GetComponent<TextMeshPro>().text = CommandPrefix + suggestion;
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

    public static string GetClosestCommand(string typedCommand)
    {
        var closestCommand = CommandListHelper().FirstOrDefault(c => c.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase));

        if (GameState.IsDev && Main.ReleaseBuildType == ReleaseTypes.Dev)
            closestCommand ??= DebugCommandListHelper().FirstOrDefault(c => c.StartsWith(typedCommand, StringComparison.OrdinalIgnoreCase));

        return closestCommand ?? string.Empty;
    }

    public static bool IsMatch(string typedCommand, string closestCommand) => closestCommand.Length >= typedCommand.Length &&
        closestCommand.Substring(0, typedCommand.Length).Equals(typedCommand, System.StringComparison.OrdinalIgnoreCase);
}

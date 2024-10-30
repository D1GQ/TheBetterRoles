using HarmonyLib;
using Hazel;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;
using static Il2CppSystem.Globalization.CultureInfo;

namespace TheBetterRoles.Modules;

public enum RpcAction : int
{
    ResetAbilityState,
    SetRole,
    EndGame,
    ReportBody,
    Vent,
    BootVent,
    Revive,
    Murder,
    PlayerPress,
    PlayerMenu,
    GuessPlayer
}

public enum CustomRPC : int
{
    // Cheat RPC's
    Sicko = 420, // Results in 164
    AUM = 42069, // Results in 85
    AUMChat = 101,

    // The Better Roles RPC's
    VersionRequest = 105,
    VersionAccept,
    SyncAllSettings,
    SyncOption,
    RoleAction,
    SyncAction,
    SyncRole,
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class PlayerControlRPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (RPC.HandleRPC(__instance, callId, reader) == false)
        {
            return false;
        }

        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        RPC.HandleCustomRPC(__instance, callId, reader);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal class PlayerPhysicsRPCHandlerPatch
{
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        RPC.HandleRPC(__instance.myPlayer, callId, reader);
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
public static class MessageReaderUpdateSystemPatch
{
    public static bool Prefix(/*ShipStatus __instance,*/ [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (GameState.IsHideNSeek) return false;

        var amount = MessageReader.Get(reader).ReadByte();

        return true;
    }
}

internal static class RPC
{
    public static void SendModRequest()
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionRequest, SendOption.None, -1);
        messageWriter.Write(Main.GetVersionText().Replace(" ", "."));
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public static void SendModAccept()
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionAccept, SendOption.None, -1);
        messageWriter.Write(Main.GetVersionText().Replace(" ", "."));
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public enum SettingType
    {
        Bool,
        Float,
        Int
    }

    public static void SyncAllSettings(PlayerControl? player = null)
    {
        if (!GameState.IsHost) return;

        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllSettings, SendOption.Reliable, player != null ? player.Data.ClientId : -1);
        writer.Write(Main.modSignature);

        List<int> ids = [];
        int count = 0;

        // Main buffer for Float, Int, and Id data
        using (var buffer = new MemoryStream())
        using (var binaryWriter = new BinaryWriter(buffer))
        {
            // Buffer for Bool values
            List<byte> boolBuffer = [];
            byte boolByte = 0;
            int boolIndex = 0;

            foreach (var item in BetterOptionItem.BetterOptionItems)
            {
                if (ids.Contains(item.Id)) continue;

                if (item is BetterOptionFloatItem floatItem && floatItem.CurrentValue != floatItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Float);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write((float)Math.Round(floatItem.CurrentValue, 5));
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is BetterOptionIntItem intItem && intItem.CurrentValue != intItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Int);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(intItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is BetterOptionCheckboxItem checkboxItem && checkboxItem.IsChecked != checkboxItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Bool);
                    binaryWriter.Write(item.Id);
                    // Pack the boolean into the boolByte
                    if (checkboxItem.IsChecked)
                    {
                        boolByte |= (byte)(1 << boolIndex); // Set the bit for true
                    }
                    boolIndex++;

                    // If we've packed 8 booleans, store the byte and reset
                    if (boolIndex == 8)
                    {
                        boolBuffer.Add(boolByte);
                        boolByte = 0; // Reset for the next byte
                        boolIndex = 0;
                    }

                    ids.Add(item.Id);
                    count++;
                }
                else if (item is BetterOptionPercentItem percentItem && percentItem.CurrentValue != percentItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Float); // Percent treated as Float
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(percentItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is BetterOptionStringItem stringItem && stringItem.CurrentValue != stringItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Int); // StringItem treated as Int
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(stringItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
            }

            // If there are any remaining booleans that weren't added
            if (boolIndex > 0)
            {
                boolBuffer.Add(boolByte); // Add the last byte if it's not full
            }

            writer.Write(count);
            writer.Write(buffer.ToArray()); // Write main data buffer
            writer.Write(boolBuffer.Count);  // Write the number of packed Bool bytes
            writer.Write(boolBuffer.ToArray()); // Write the packed Bool buffer as separate data
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SyncOption(int id, string data, string value)
    {
        if (!GameState.IsHost) return;

        var text = Utils.SettingsChangeNotifier(id, value);

        var writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncOption, SendOption.Reliable);
        writer.Write(Main.modSignature);
        writer.Write(id);
        writer.Write(data);
        writer.Write(text);
        writer.EndMessage();
    }

    public static void HandleCustomRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null || Enum.IsDefined(typeof(RpcCalls), callId)) return;

        if (Enum.IsDefined(typeof(CustomRPC), (int)unchecked(callId)))
        {
            MessageReader reader = MessageReader.Get(oldReader);

            switch ((CustomRPC)callId)
            {
                case CustomRPC.VersionRequest:
                    {
                        var version = reader.ReadString();

                        player.BetterData().HasMod = true;
                        player.BetterData().Version = version;

                        if (version == Main.GetVersionText().Replace(" ", "."))
                        {
                            PlayerControl.LocalPlayer.BetterData().HasMod = true;
                            PlayerControl.LocalPlayer.BetterData().Version = Main.GetVersionText().Replace(" ", ".");
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.BetterData().KickTimer = 8f;
                            PlayerControl.LocalPlayer.BetterData().MismatchVersion = true;
                            PlayerControl.LocalPlayer.BetterData().HasMod = true;
                            PlayerControl.LocalPlayer.BetterData().Version = Main.GetVersionText().Replace(" ", ".");
                        }
                        SendModAccept();
                    }
                    break;
                case CustomRPC.VersionAccept:
                    {
                        var version = reader.ReadString();

                        if (version == Main.GetVersionText().Replace(" ", "."))
                        {
                            player.BetterData().HasMod = true;
                            player.BetterData().Version = version;

                            if (GameState.IsHost)
                            {
                                SyncAllSettings(player);
                            }
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.BetterData().KickTimer = 8f;
                            player.BetterData().MismatchVersion = true;
                            player.BetterData().HasMod = true;
                            player.BetterData().Version = version;
                            TBRLogger.InGame(string.Format(Translator.GetString("VersionMismatch"), player.Data.PlayerName, (int)player.BetterData().KickTimer));
                        }
                    }
                    break;
                case CustomRPC.SyncAllSettings:
                    {
                        var signature = reader.ReadString();
                        if (!GameState.IsHost && signature == Main.modSignature && player.IsHost())
                        {
                            BetterDataManager.HostSettings.Clear();
                            GameSettingMenuPatch.SetupSettings(true);

                            int count = reader.ReadInt32(); // Read the number of settings

                            // First, read the main data buffer
                            Dictionary<int, string> settings = new Dictionary<int, string>();
                            for (int i = 0; i < count; i++)
                            {
                                SettingType settingType = (SettingType)reader.ReadByte();
                                int id = reader.ReadInt32();

                                switch (settingType)
                                {
                                    case SettingType.Float:
                                        float floatValue = reader.ReadSingle();
                                        settings.Add(id, floatValue.ToString());
                                        break;

                                    case SettingType.Int:
                                        int intValue = reader.ReadInt32();
                                        settings.Add(id, intValue.ToString());
                                        break;

                                    case SettingType.Bool:
                                        // We only read the Id here, the actual Bool value will be read from the Bool buffer later
                                        settings.Add(id, "Bool"); // Placeholder to identify this as a Bool
                                        break;
                                }
                            }

                            // Next, read the Bool data buffer
                            int boolBufferLength = reader.ReadInt32();
                            byte[] boolData = reader.ReadBytes(boolBufferLength);

                            // Process Bool values
                            int boolByteCount = boolData.Length;
                            int boolIndex = 0;

                            foreach (var kvp in settings.ToList()) // Convert to list to allow modification
                            {
                                if (kvp.Value == "Bool") // Check for Bool placeholder
                                {
                                    // Calculate which byte and bit to read
                                    int byteIndex = boolIndex / 8;
                                    if (byteIndex < boolByteCount)
                                    {
                                        bool boolValue = (boolData[byteIndex] & 1 << boolIndex % 8) != 0; // Check the specific bit
                                        settings[kvp.Key] = boolValue.ToString();
                                    }
                                    boolIndex++;
                                }
                            }

                            // Save settings
                            foreach (var kvp in settings)
                            {
                                BetterDataManager.SaveSetting(kvp.Key, kvp.Value);
                                BetterOptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == kvp.Key)?.SyncValue(kvp.Value);
                            }
                        }
                    }
                    break;
                case CustomRPC.SyncOption:
                    {
                        var signature = reader.ReadString();
                        if (!GameState.IsHost && signature == Main.modSignature && player.IsHost())
                        {
                            int Id = reader.ReadInt32();
                            string data = reader.ReadString();
                            string text = reader.ReadString();

                            BetterDataManager.SaveSetting(Id, data);
                            BetterOptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == Id)?.SyncValue(data);
                            Utils.SettingsChangeNotifierSync(Id, text);
                        }
                    }
                    break;
                case CustomRPC.RoleAction:
                case CustomRPC.SyncRole:
                    {
                        var User = reader.ReadNetObject<PlayerControl>();
                        var roleType = (CustomRoles)reader.ReadInt32();
                        if (User != null)
                        {
                            CustomRoleManager.RoleListener(User, role => role.HandleRpc(oldReader, callId, User, player), role => role.RoleType == roleType);
                        }
                    }
                    break;
                case CustomRPC.SyncAction:
                    HandleSyncActionRPC(player, oldReader, true);
                    break;
            }
        }
    }

    public static bool HandleRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.Data == null) return true;

        MessageReader reader = MessageReader.Get(oldReader);

        switch ((RpcCalls)callId)
        {
            case RpcCalls.CompleteTask:
                {
                    uint Id = reader.ReadUInt32();
                    CustomRoleManager.RoleListenerOther(role => role.OnTaskCompleteOther(player, Id));
                }
                break;
        }

        return true;
    }

    public static void HandleSyncActionRPC(PlayerControl sender, MessageReader oldReader, bool IsRPC = false)
    {
        MessageReader reader = MessageReader.Get(oldReader);

        var signature = reader.ReadString();
        var action = reader.ReadInt32();
        var player = reader.ReadNetObject<PlayerControl>();

        if (Main.modSignature != signature) return;

        ActionRPCs.SenderPlayer = sender;

        switch ((RpcAction)action)
        {
            case RpcAction.EndGame:
                {
                    List<byte> winnersIds = [];
                    var reason = (EndGameReason)reader.ReadByte();
                    var team = (CustomRoleTeam)reader.ReadByte();
                    int winnersAmount = reader.ReadInt32();
                    for (int i = 0; i < winnersAmount; i++)
                    {
                        winnersIds.Add(reader.ReadByte());
                    }

                    ActionRPCs.EndGameSync(winnersIds, reason, team, IsRPC);
                }
                break;
            case RpcAction.ResetAbilityState:
                {
                    var id = reader.ReadInt32();
                    var roleType = reader.ReadInt32();
                    var isTimeOut = reader.ReadBoolean();
                    player.ResetAbilityStateSync(id, roleType, isTimeOut, IsRPC);
                }
                break;
            case RpcAction.SetRole:
                {
                    var role = reader.ReadInt32();
                    var RemoveAddon = reader.ReadBoolean();
                    player.SetRoleSync((CustomRoles)role, RemoveAddon, IsRPC);
                }
                break;
            case RpcAction.ReportBody:
                {
                    var bodyInfo = reader.ReadNetObject<NetworkedPlayerInfo>();
                    player.ReportBodySync(bodyInfo, IsRPC);
                }
                break;
            case RpcAction.Revive:
                {
                    player.ReviveSync(IsRPC);
                }
                break;
            case RpcAction.Murder:
                {
                    var target = reader.ReadNetObject<PlayerControl>();
                    bool isAbility = reader.ReadBoolean();
                    byte flags = reader.ReadByte();

                    bool snapToTarget = (flags & (byte)MultiMurderFlags.snapToTarget) != 0;
                    bool spawnBody = (flags & (byte)MultiMurderFlags.spawnBody) != 0;
                    bool showAnimation = (flags & (byte)MultiMurderFlags.showAnimation) != 0;
                    bool playSound = (flags & (byte)MultiMurderFlags.playSound) != 0;

                    if (target != null)
                    {
                        player.MurderSync(target, isAbility, (MultiMurderFlags)flags, IsRPC);
                    }
                }
                break;

            case RpcAction.PlayerMenu:
                {
                    var Id = reader.ReadInt32();
                    var roleType = reader.ReadInt32();
                    var close = reader.ReadBoolean();
                    var target = reader.ReadNetObject<NetworkedPlayerInfo>();
                    if (target != null)
                    {
                        player.PlayerMenuSync(Id, roleType, target, null, null, close, IsRPC);
                    }
                }
                break;
            case RpcAction.PlayerPress:
                {
                    var target = reader.ReadNetObject<PlayerControl>();
                    if (target != null)
                    {
                        player.PlayerPressSync(target, IsRPC);
                    }
                }
                break;
            case RpcAction.Vent:
                {
                    var ventId = reader.ReadByte();
                    var exit = reader.ReadBoolean();
                    player.VentSync(ventId, exit, IsRPC);
                }
                break;
            case RpcAction.GuessPlayer:
                {
                    var target = reader.ReadNetObject<PlayerControl>();
                    var roleType = (CustomRoles)reader.ReadInt32();
                    if (target != null)
                    {
                        player.GuessPlayerSync(target, roleType, IsRPC);
                    }
                }
                break;
        }
    }
}


using HarmonyLib;
using Hazel;
using InnerNet;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

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
    PlayerPress
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
    SyncSettings,
    SyncOption,
    RoleAction,
    SyncAction,
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
        if (GameStates.IsHideNSeek) return false;

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

    public static void SyncSettings(PlayerControl? player = null)
    {
        if (!GameStates.IsHost) return;

        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncSettings, SendOption.Reliable, player != null ? player.Data.ClientId : -1);
        writer.Write(Main.modSignature);

        List<int> Ids = [];
        Dictionary<int, string> settings = [];
        int count = 0;
        foreach (var item in BetterOptionItem.BetterOptionItems)
        {
            if (item is BetterOptionFloatItem option1 && option1.CurrentValue != option1.defaultValue)
            {
                if (Ids.Contains(item.Id)) continue;
                settings[item.Id] = option1.CurrentValue.ToString();
                Ids.Add(item.Id);
                count++;
            }
            else if (item is BetterOptionIntItem option2 && option2.CurrentValue != option2.defaultValue)
            {
                if (Ids.Contains(item.Id)) continue;
                settings[item.Id] = option2.CurrentValue.ToString();
                Ids.Add(item.Id);
                count++;
            }
            else if (item is BetterOptionCheckboxItem option3 && option3.IsChecked != option3.defaultValue)
            {
                if (Ids.Contains(item.Id)) continue;
                settings[item.Id] = option3.IsChecked.ToString() ?? false.ToString();
                Ids.Add(item.Id);
                count++;
            }
            else if (item is BetterOptionPercentItem option4 && option4.CurrentValue != option4.defaultValue)
            {
                if (Ids.Contains(item.Id)) continue;
                settings[item.Id] = option4.CurrentValue.ToString();
                Ids.Add(item.Id);
                count++;
            }
            else if (item is BetterOptionStringItem option5 && option5.CurrentValue != option5.defaultValue)
            {
                if (Ids.Contains(item.Id)) continue;
                settings[item.Id] = option5.CurrentValue.ToString();
                Ids.Add(item.Id);
                count++;
            }
        }

        writer.Write(count);

        foreach (var kvp in settings)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SyncOption(int id, string data, string value)
    {
        if (!GameStates.IsHost) return;

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

                            if (GameStates.IsHost)
                            {
                                SyncSettings(player);
                            }
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.BetterData().KickTimer = 8f;
                            player.BetterData().MismatchVersion = true;
                            player.BetterData().HasMod = true;
                            player.BetterData().Version = version;
                            Logger.InGame(string.Format(Translator.GetString("VersionMismatch"), player.Data.PlayerName, (int)player.BetterData().KickTimer));
                        }
                    }
                    break;
                case CustomRPC.SyncSettings:
                    {
                        var signature = reader.ReadString();
                        if (!GameStates.IsHost && signature == Main.modSignature && player.IsHost())
                        {
                            BetterDataManager.HostSettings.Clear();
                            GameSettingMenuPatch.SetupSettings(true);
                            int count = reader.ReadInt32();

                            Dictionary<int, string> settings = [];
                            for (int i = 0; i < count; i++)
                            {
                                int Id = reader.ReadInt32();
                                string data = reader.ReadString();
                                settings.Add(Id, data);
                            }

                            foreach (var kvp in settings)
                            {
                                BetterDataManager.SaveSetting(kvp.Key, kvp.Value);
                            }
                        }
                    }
                    break;
                case CustomRPC.SyncOption:
                    {
                        var signature = reader.ReadString();
                        if (!GameStates.IsHost && signature == Main.modSignature && player.IsHost())
                        {
                            int Id = reader.ReadInt32();
                            string data = reader.ReadString();
                            string text = reader.ReadString();

                            BetterDataManager.SaveSetting(Id, data);
                            Utils.SettingsChangeNotifierSync(Id, text);
                        }
                    }
                    break;
                case CustomRPC.RoleAction:
                    {
                        var User = reader.ReadNetObject<PlayerControl>();
                        var RoleType = (CustomRoles)reader.ReadInt32();
                        if (User != null)
                        {
                            if (User.BetterData().RoleInfo.RoleAssigned && User.BetterData().RoleInfo.Role.RoleType == RoleType)
                            {
                                User.BetterData().RoleInfo.Role.HandleRpc(oldReader, callId, User, player);
                            }
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
                    byte flags = reader.ReadByte();
                    var isAbility = (flags & (1 << 0)) != 0;
                    var snapToTarget = (flags & (1 << 1)) != 0;
                    var spawnBody = (flags & (1 << 2)) != 0;
                    var showAnimation = (flags & (1 << 3)) != 0;
                    var playSound = (flags & (1 << 4)) != 0;

                    if (target != null)
                    {
                        player.MurderSync(target, isAbility, snapToTarget, spawnBody, showAnimation, playSound, IsRPC);
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
                    var ventId = reader.ReadInt32();
                    var exit = reader.ReadBoolean();
                    player.VentSync(ventId, exit, IsRPC);
                }
                break;
        }
    }
}


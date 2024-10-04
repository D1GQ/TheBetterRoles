using HarmonyLib;
using Hazel;
using InnerNet;

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
    VersionCheck = 105,
    SyncSettings,
    RoleAction,
    CheckAction,
    Action,
    SyncAction,
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class PlayerControlRPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        RPC.HandleRPC(__instance, callId, reader);

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
    public static void SendBetterRoleCheck()
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.None, -1);
        messageWriter.Write(true);
        messageWriter.Write(Main.modSignature);
        messageWriter.Write(Main.GetVersionText().Replace(" ", ""));
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public static void HandleCustomRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null || Enum.IsDefined(typeof(RpcCalls), callId)) return;

        if (Enum.IsDefined(typeof(CustomRPC), (int)unchecked(callId)))
        {
            MessageReader reader = MessageReader.Get(oldReader);

            switch ((CustomRPC)callId)
            {
                case CustomRPC.VersionCheck:
                    {
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
                case CustomRPC.CheckAction:
                    HandleActionRPC(player, oldReader, true);
                    break;
                case CustomRPC.Action:
                    HandleActionRPC(player, oldReader, false);
                    break;
                case CustomRPC.SyncAction:
                    HandleSyncActionRPC(player, oldReader, true);
                    break;
            }
        }
    }

    public static void HandleRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null) return;

        MessageReader reader = MessageReader.Get(oldReader);

        switch (callId)
        {
        }
    }

    public static void HandleActionRPC(PlayerControl sender, MessageReader oldReader, bool IsCheck)
    {
        if (!IsCheck && GameStates.IsHost || IsCheck && !GameStates.IsHost) return;

        MessageReader reader = MessageReader.Get(oldReader);

        var signature = reader.ReadString();
        var action = reader.ReadInt32();
        var player = reader.ReadNetObject<PlayerControl>();

        if (Main.modSignature != signature) return;

        ActionRPCs.SenderPlayer = sender;

        var hostFlag = sender.IsHost();

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

                    ActionRPCs.EndGameSync(winnersIds, reason, team);
                }
                break;
            case RpcAction.ResetAbilityState:
                {
                    var id = reader.ReadInt32();
                    player.ResetAbilityStateSync(id, hostFlag);
                }
                break;
            case RpcAction.SetRole:
                {
                    var role = reader.ReadInt32();
                    var RemoveAddon = reader.ReadBoolean();
                    player.SetRoleSync((CustomRoles)role, RemoveAddon, hostFlag);
                }
                break;
            case RpcAction.ReportBody:
                {
                    var bodyInfo = reader.ReadNetObject<NetworkedPlayerInfo>();
                    player.ReportBodySync(bodyInfo, hostFlag);
                }
                break;
            case RpcAction.Vent:
                {
                    var ventId = reader.ReadInt32();
                    var exit = reader.ReadBoolean();
                    player.VentSync(ventId, exit);
                }
                break;
            case RpcAction.Revive:
                {
                    player.ReviveSync(hostFlag);
                }
                break;
            case RpcAction.Murder:
                {
                    var target = reader.ReadNetObject<PlayerControl>();
                    var isAbility = reader.ReadBoolean();
                    if (target != null)
                    {
                        player.MurderSync(target, isAbility, hostFlag);
                    }
                }
                break;
            case RpcAction.PlayerPress:
                {
                    var target = reader.ReadNetObject<PlayerControl>();
                    if (target != null)
                    {
                        player.PlayerPressSync(target, hostFlag);
                    }
                }
                break;
        }
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


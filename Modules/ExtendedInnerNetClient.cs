using Hazel;
using InnerNet;
namespace TheBetterRoles;

static class ExtendedInnerNetClient
{
    public static MessageWriter StartActionRpc(this InnerNetClient client, RpcAction action, PlayerControl? asPlayer = null)
    {
        var LocalPlayer = PlayerControl.LocalPlayer;

        MessageWriter writer;
        if (GameStates.IsHost)
        {
            writer = client.StartRpcImmediately(LocalPlayer.NetId, (byte)CustomRPC.Action, SendOption.Reliable, -1);
            writer.Write(Main.modSignature);
            writer.Write((int)action);
            writer.WriteNetObject(asPlayer ?? LocalPlayer);
        }
        else
        {
            writer = client.StartRpcImmediately(LocalPlayer.NetId, (byte)CustomRPC.CheckAction, SendOption.Reliable, AmongUsClient.Instance.GetHost().Id);
            writer.Write(Main.modSignature);
            writer.Write((int)action);
            writer.WriteNetObject(LocalPlayer);
        }

        return writer;
    }

    public static void EndActionRpc(this InnerNetClient client, MessageWriter writer) => client.FinishRpcImmediately(writer);

    /// <summary>
    /// Starts the RPC desynchronization process for the given player, call ID, and send option.
    /// </summary>
    /// <param name="client">The InnerNetClient instance.</param>
    /// <param name="playerNetId">The network ID of the player.</param>
    /// <param name="callId">The RPC call ID.</param>
    /// <param name="option">The send option for the RPC.</param>
    /// <param name="ignoreClientId">The client ID to ignore. Default is -1, which means no client is ignored.</param>
    /// <returns>A list of MessageWriter instances for the RPC calls.</returns>
    /// <example>
    /// <code>
    /// List&lt;MessageWriter&gt; messageWriter = AmongUsClient.Instance.StartRpcDesync(PlayerNetId, (byte)RpcCalls, SendOption, ClientId);
    /// messageWriter.ForEach(mW => mW.Write("RPC TEST"));
    /// AmongUsClient.Instance.FinishRpcDesync(messageWriter);
    /// </code>
    /// </example>
    /*
            List<MessageWriter> messageWriter = AmongUsClient.Instance.StartRpcDesync(PlayerNetId, (byte)RpcCalls, SendOption, ClientId);
            messageWriter.ForEach(mW => mW.Write("RPC TEST"));
            AmongUsClient.Instance.FinishRpcDesync(messageWriter);
    */
    public static List<MessageWriter> StartRpcDesync(this InnerNetClient client, uint playerNetId, byte callId, SendOption option, int ignoreClientId = -1, Func<ClientData, bool> clientCheck = null)
    {
        List<MessageWriter> messageWriters = new List<MessageWriter>();

        if (ignoreClientId < 0)
        {
            messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, -1));
        }
        else
        {
            foreach (var allClients in AmongUsClient.Instance.allClients.ToArray().Where(c => c.Id != ignoreClientId))
            {
                if (clientCheck == null || clientCheck.Invoke(allClients))
                {
                    messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, allClients.Id));
                }
            }
        }

        return messageWriters;
    }



    public static void FinishRpcDesync(this InnerNetClient client, List<MessageWriter> messageWriters)
    {
        foreach (var msg in messageWriters)
        {
            msg.EndMessage();
            msg.EndMessage();
            client.SendOrDisconnect(msg);
            msg.Recycle();
        }
    }

}

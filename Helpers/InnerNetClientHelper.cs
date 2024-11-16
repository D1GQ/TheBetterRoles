using Hazel;
using InnerNet;

namespace TheBetterRoles.Helpers;

static class InnerNetClientHelper
{
    public static void WriteBooleans(this MessageWriter writer, bool[] @bools)
    {
        writer.Write(@bools.Length);

        byte currentByte = 0;
        int bitIndex = 0;

        foreach (bool b in @bools)
        {
            if (b)
                currentByte |= (byte)(1 << bitIndex);

            bitIndex++;

            if (bitIndex == 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitIndex = 0;
            }
        }

        if (bitIndex > 0)
        {
            writer.Write(currentByte);
        }
    }


    public static void ReadBooleans(this MessageReader reader, ref bool[] @bools)
    {
        var length = reader.ReadInt32();

        int bitIndex = 0;
        byte currentByte = 0;

        for (int i = 0; i < length; i++)
        {
            if (bitIndex == 0)
            {
                currentByte = reader.ReadByte();
            }

            @bools[i] = (currentByte & 1 << bitIndex) != 0;

            bitIndex++;

            if (bitIndex == 8)
            {
                bitIndex = 0;
            }
        }
    }

    public static void WritePlayerId(this MessageWriter writer, PlayerControl player) => writer.Write(player?.PlayerId ?? 255);

    public static PlayerControl? ReadPlayerId(this MessageReader reader) => Utils.PlayerFromPlayerId(reader.ReadByte());

    public static void WritePlayerDataId(this MessageWriter writer, NetworkedPlayerInfo data) => writer.Write(data?.PlayerId ?? 255);

    public static NetworkedPlayerInfo? ReadPlayerDataId(this MessageReader reader) => Utils.PlayerDataFromPlayerId(reader.ReadByte());

    public static void WriteDeadBodyId(this MessageWriter writer, DeadBody body) => writer.Write(body?.ParentId ?? 255);

    public static DeadBody? ReadDeadBodyId(this MessageReader reader) => Main.AllDeadBodys.FirstOrDefault(deadbody => deadbody.ParentId == reader.ReadByte());

    public static void WriteVentId(this MessageWriter writer, Vent vent) => writer.WritePacked(vent?.Id ?? -1);

    public static Vent? ReadVentId(this MessageReader reader) => Main.AllVents.FirstOrDefault(vent => vent.Id == reader.ReadPackedInt32());

    public static MessageWriter Copy(this MessageWriter writer)
    {
        var list = new Il2CppSystem.Collections.Generic.Stack<int>();
        foreach (int i in writer.messageStarts)
        {
            list.Push(i);
        }
        var msg = MessageWriter.Get(writer.SendOption);
        msg.Position = writer.Position;
        msg.messageStarts = list;
        msg.Buffer = writer.Buffer;
        msg.Length = writer.Length;
        return msg;
    }

    /// <summary>
    /// Starts the RPC desynchronization process for the given player, call ID, and send option.
    /// </summary>
    /// <param name="client">The InnerNetClient instance.</param>
    /// <param name="playerNetId">The network ID of the player.</param>
    /// <param name="callId">The RPC call ID.</param>
    /// <param name="option">The send option for the RPC.</param>
    /// <param name="ignoreClientIds">The client ID to ignore. Default is -1, which means no client is ignored.</param>
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
    public static List<MessageWriter> StartRpcDesync(this InnerNetClient client, uint playerNetId, byte callId, SendOption option, int[] ignoreClientIds, Func<ClientData, bool>? clientCheck = null)
    {
        List<MessageWriter> messageWriters = [];

        if (ignoreClientIds.All(Int => Int < 0))
        {
            messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, -1));
        }
        else
        {
            foreach (var allClients in AmongUsClient.Instance.allClients.ToArray().Where(c => !ignoreClientIds.Contains(c.Id)))
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

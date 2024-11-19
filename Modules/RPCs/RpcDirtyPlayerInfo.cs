using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.DirtyPlayerInfo)]
public class RpcDirtyPlayerInfo(Main plugin, uint id) : PlayerCustomRpc<Main, RpcDirtyPlayerInfo.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(ExtendedPlayerInfo extendedData, MessageReader? reader = null)
    {
        public readonly MessageReader? Reader = reader;
        public readonly ExtendedPlayerInfo ExtendedData = extendedData;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        ExtendedPlayerInfo extendedData = data.ExtendedData;
        writer.Write(extendedData._PlayerId);
        writer.WritePacked((int)extendedData.DeathReason);
        writer.Write(Utils.Color32ToHex(extendedData.DeathReasonColor));
        writer.Write(extendedData.IsFakeAlive);
        writer.Write(extendedData.CamouflagedQueue.GetQueueCount());
        writer.Write(extendedData.CosmeticsActiveQueue.GetQueueCount());
        writer.WritePacked(extendedData?.DisconnectReason != null ? (int)extendedData.DisconnectReason : -1);
    }

    public override Data Read(MessageReader reader)
    {
        var extendedData = reader.ReadPlayerDataId().ExtendedData();
        return new Data(extendedData, reader);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        var reader = data.Reader;
        ExtendedPlayerInfo extendedData = data.ExtendedData;

        extendedData.DeathReason = (DeathReasons)reader.ReadPackedInt32();
        extendedData.DeathReasonColor = Utils.HexToColor32(reader.ReadString());
        extendedData.IsFakeAlive = reader.ReadBoolean();
        extendedData.CamouflagedQueue.SetQueueCount(reader.ReadUInt32());
        extendedData.CosmeticsActiveQueue.SetQueueCount(reader.ReadUInt32());
        var disconnectInt = reader.ReadPackedInt32();
        extendedData.DisconnectReason = disconnectInt >= 0 ? (DisconnectReasons)disconnectInt : null;
    }
}

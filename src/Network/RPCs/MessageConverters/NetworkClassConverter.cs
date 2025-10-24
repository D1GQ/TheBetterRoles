using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Serialization;

namespace TheBetterRoles.Network.RPCs.MessageConverters;

[MessageConverter]
internal sealed class NetworkClassConverter : MessageConverter<NetworkClass>
{
    public override NetworkClass Read(MessageReader reader, Type objectType)
    {
#pragma warning disable CS8603
        return NetworkClass.GetFromNetId(reader.ReadPackedUInt32());
#pragma warning restore CS8603
    }

    public override void Write(MessageWriter writer, NetworkClass value)
    {
        writer.WritePacked(value.NetworkId);
    }
}

[MessageConverter]
internal sealed class NetworkClassMonoConverter : MessageConverter<NetworkClassMono>
{
    public override NetworkClassMono Read(MessageReader reader, Type objectType)
    {
#pragma warning disable CS8603
        return NetworkClassMono.GetFromNetId(reader.ReadPackedUInt32());
#pragma warning restore CS8603
    }

    public override void Write(MessageWriter writer, NetworkClassMono value)
    {
        writer.WritePacked(value.NetworkId);
    }
}
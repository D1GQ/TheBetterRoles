using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Serialization;
using TheBetterRoles.Items;

namespace TheBetterRoles.Network.RPCs.MessageConverters;

[MessageConverter]
internal sealed class ObjectHashConverter : MessageConverter<ObjectHash>
{
    public override ObjectHash Read(MessageReader reader, Type objectType)
    {
        var hash = reader.ReadUInt16();
        return ObjectHashExtension.GetObjectByHash(hash);
    }

    public override void Write(MessageWriter writer, ObjectHash value)
    {
        writer.Write(value.Hash);
    }
}
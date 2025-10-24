using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Serialization;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Network.RPCs.MessageConverters;

[MessageConverter]
internal sealed class DeadBodyConverter : MessageConverter<DeadBody>
{
    public override DeadBody Read(MessageReader reader, Type objectType)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return reader.ReadDeadBody();
#pragma warning restore CS8603 // Possible null reference return.
    }

    public override void Write(MessageWriter writer, DeadBody value)
    {
        writer.WriteDeadBody(value);
    }
}
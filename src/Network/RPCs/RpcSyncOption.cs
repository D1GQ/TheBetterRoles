using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.SyncOption)]
internal class RpcSyncOption(Main plugin, uint id) : PlayerCustomRpc<Main, RpcSyncOption.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(int Id, object value)
    {
        public readonly int Id = Id;
        public readonly object Value = value;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        ArgumentNullException.ThrowIfNull(data.Value, nameof(data.Value));
        int id = data.Id;
        object? value = data.Value;

        writer.WritePacked(id);
        if (value is float @float)
        {
            writer.Write((byte)1);
            writer.Write(@float);
        }
        else if (value is int @int)
        {
            writer.Write((byte)2);
            writer.Write(@int);
        }
        else if (value is bool @bool)
        {
            writer.Write((byte)3);
            writer.Write(@bool);
        }
        else
        {
            writer.Write((byte)0);
        }
    }

    public override Data Read(MessageReader reader)
    {
        int id = reader.ReadPackedInt32();
        byte type = reader.ReadByte();
        object? value = null;
        if (type == 1)
        {
            value = reader.ReadSingle();
        }
        else if (type == 2)
        {
            value = reader.ReadInt32();
        }
        else if (type == 3)
        {
            value = reader.ReadBoolean();
        }

        return new Data(id, value);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        if (!GameState.IsHost && player.IsHost())
        {
            int Id = data.Id;
            var value = data.Value;

            OptionItem.GetOptionById(Id)?.SyncValue(value);
        }
    }
}

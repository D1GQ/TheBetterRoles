using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.AddChatPrivate)]
internal class RpcAddChatPrivate(Main plugin, uint id) : PlayerCustomRpc<Main, RpcAddChatPrivate.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(string text, string overrideName = "", bool setRight = false)
    {
        public readonly string Text = text;
        public readonly string OverrideName = overrideName;
        public readonly bool SetRight = setRight;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.Text);
        writer.Write(data.OverrideName);
        writer.Write(data.SetRight);
    }

    public override Data Read(MessageReader reader)
    {
        var text = reader.ReadString();
        var overrideName = reader.ReadString();
        var setRight = reader.ReadBoolean();
        return new Data(text, overrideName, setRight);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        Utils.AddChatPrivate(data.Text, data.OverrideName, data.SetRight);
    }
}

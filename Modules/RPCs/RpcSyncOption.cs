using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.RPCs
{
    [RegisterCustomRpc((uint)ReactorRPCs.SyncOption)]
    public class RpcSyncOption : PlayerCustomRpc<Main, RpcSyncOption.Data>
    {
        public override SendOption SendOption => SendOption.Reliable;
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
        public RpcSyncOption(Main plugin, uint id) : base(plugin, id)
        {
        }

        public readonly struct Data(int Id, string value, string text)
        {
            public readonly int Id = Id;
            public readonly string value = value;
            public readonly string text = text;
        }

        public override void Write(MessageWriter writer, Data data)
        {
            var id = data.Id;
            var value = data.value;
            var text = Utils.SettingsChangeNotifier(id, data.text);

            writer.WritePacked(id);
            writer.Write(value);
            writer.Write(text);
        }

        public override Data Read(MessageReader reader)
        {
            return new Data(reader.ReadPackedInt32(), reader.ReadString(), reader.ReadString());
        }

        public override void Handle(PlayerControl player, Data data)
        {
            if (!GameState.IsHost && player.IsHost())
            {
                int Id = data.Id;
                var value = data.value;
                var text = data.text;

                TBRDataManager.SaveSetting(Id, value);
                TBROptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == Id)?.SyncValue(value);
                Utils.SettingsChangeNotifierSync(Id, text);
            }
        }
    }
}

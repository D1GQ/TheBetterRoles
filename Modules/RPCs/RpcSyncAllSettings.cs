using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.RPCs;

public enum SettingType
{
    Bool,
    Float,
    Int
}

[RegisterCustomRpc((uint)ReactorRPCs.SyncAllSettings)]
public class RpcSyncAllSettings(Main plugin, uint id) : PlayerCustomRpc<Main, RpcSyncAllSettings.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(Dictionary<int, string>? settings = null, bool[]? boolData = null)
    {
        public readonly Dictionary<int, string>? Settings = settings;
        public readonly bool[]? BoolData = boolData;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        List<int> ids = new List<int>();
        int count = 0;

        if (TBROptionItem.BetterOptionItems == null) return;

        using (var buffer = new MemoryStream())
        using (var binaryWriter = new BinaryWriter(buffer))
        {
            List<bool> bools = new List<bool>();

            foreach (var item in TBROptionItem.BetterOptionItems)
            {
                if (ids.Contains(item.Id)) continue;

                if (item is TBROptionFloatItem floatItem && floatItem.CurrentValue != floatItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Float);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write((float)Math.Round(floatItem.CurrentValue, 5));
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is TBROptionIntItem intItem && intItem.CurrentValue != intItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Int);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(intItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is TBROptionCheckboxItem checkboxItem && checkboxItem.IsChecked != checkboxItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Bool);
                    binaryWriter.Write(item.Id);
                    bools.Add(checkboxItem.IsChecked); // Collect boolean values
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is TBROptionPercentItem percentItem && percentItem.CurrentValue != percentItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Float);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(percentItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
                else if (item is TBROptionStringItem stringItem && stringItem.CurrentValue != stringItem.defaultValue)
                {
                    binaryWriter.Write((byte)SettingType.Int);
                    binaryWriter.Write(item.Id);
                    binaryWriter.Write(stringItem.CurrentValue);
                    ids.Add(item.Id);
                    count++;
                }
            }

            writer.Write(count);
            writer.Write(buffer.ToArray());
            writer.WriteBooleans([.. bools]);
        }
    }

    public override Data Read(MessageReader reader)
    {
        Dictionary<int, string> settings = new Dictionary<int, string>();

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            SettingType settingType = (SettingType)reader.ReadByte();
            int id = reader.ReadInt32();

            switch (settingType)
            {
                case SettingType.Float:
                    float floatValue = reader.ReadSingle();
                    settings.Add(id, floatValue.ToString());
                    break;

                case SettingType.Int:
                    int intValue = reader.ReadInt32();
                    settings.Add(id, intValue.ToString());
                    break;

                case SettingType.Bool:
                    settings.Add(id, "Bool");
                    break;
            }
        }

        bool[] bools = reader.ReadBooleans();

        return new Data(settings, bools);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        if (player.IsHost())
        {
            TBRDataManager.HostSettings.Clear();
            GameSettingMenuPatch.SetupSettings(true, true);

            int boolIndex = 0;
            foreach (var kvp in data.Settings.ToList())
            {
                if (kvp.Value == "Bool")
                {
                    if (data.BoolData != null && boolIndex < data.BoolData.Length)
                    {
                        bool boolValue = data.BoolData[boolIndex];
                        data.Settings[kvp.Key] = boolValue.ToString();
                    }
                    boolIndex++;
                }
            }

            foreach (var kvp in data.Settings)
            {
                TBRDataManager.SaveSetting(kvp.Key, kvp.Value);
                TBROptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == kvp.Key)?.SyncValue(kvp.Value);
            }
        }
    }
}

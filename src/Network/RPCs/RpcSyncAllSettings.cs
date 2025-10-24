using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Monos;
using SettingType = TheBetterRoles.Items.Enums.SettingType;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.SyncAllSettings)]
internal class RpcSyncAllSettings(Main plugin, uint id) : PlayerCustomRpc<Main, RpcSyncAllSettings.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(Dictionary<int, object>? settings = null, bool[]? boolData = null)
    {
        public readonly Dictionary<int, object>? Settings = settings;
        public readonly bool[]? BoolData = boolData;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        List<int> ids = [];
        int count = 0;

        if (OptionItem.AllTBROptions == null) return;

        using var buffer = new MemoryStream();
        using var binaryWriter = new BinaryWriter(buffer);
        List<bool> bools = [];

        foreach (var item in OptionItem.AllTBROptions)
        {
            if (ids.Contains(item.Id)) continue;
            if (!item.IsOption) continue;

            if (item is OptionFloatItem floatItem && floatItem.GetValue() != floatItem.GetDefaultValue())
            {
                binaryWriter.Write((byte)SettingType.Float);
                binaryWriter.Write(item.Id);
                binaryWriter.Write((float)Math.Round(floatItem.GetValue(), 5));
                ids.Add(item.Id);
                count++;
            }
            else if (item is OptionIntItem intItem && intItem.GetValue() != intItem.GetDefaultValue())
            {
                binaryWriter.Write((byte)SettingType.Int);
                binaryWriter.Write(item.Id);
                binaryWriter.Write(intItem.GetValue());
                ids.Add(item.Id);
                count++;
            }
            else if (item is OptionCheckboxItem checkboxItem && checkboxItem.GetValue() != checkboxItem.GetDefaultValue())
            {
                binaryWriter.Write((byte)SettingType.Bool);
                binaryWriter.Write(item.Id);
                bools.Add(checkboxItem.GetValue());
                ids.Add(item.Id);
                count++;
            }
            else if (item is OptionPercentItem percentItem && percentItem.GetValue() != percentItem.GetDefaultValue())
            {
                binaryWriter.Write((byte)SettingType.Float);
                binaryWriter.Write(item.Id);
                binaryWriter.Write(percentItem.GetValue());
                ids.Add(item.Id);
                count++;
            }
            else if (item is OptionStringItem stringItem && stringItem.GetValue() != stringItem.GetDefaultValue())
            {
                binaryWriter.Write((byte)SettingType.Int);
                binaryWriter.Write(item.Id);
                binaryWriter.Write(stringItem.GetValue());
                ids.Add(item.Id);
                count++;
            }
        }

        writer.Write(CatchedGameData.lobbyTimer);
        writer.Write(count);
        writer.Write(buffer.ToArray());
        writer.WriteBooleans([.. bools]);
    }

    public override Data Read(MessageReader reader)
    {
        Dictionary<int, object> settings = [];

        CatchedGameData.lobbyTimer = reader.ReadSingle();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            SettingType settingType = (SettingType)reader.ReadByte();
            int id = reader.ReadInt32();

            switch (settingType)
            {
                case SettingType.Float:
                    float floatValue = reader.ReadSingle();
                    settings.Add(id, floatValue);
                    break;

                case SettingType.Int:
                    int intValue = reader.ReadInt32();
                    settings.Add(id, intValue);
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
            int boolIndex = 0;
            foreach (var kvp in data.Settings.ToList())
            {
                if (kvp.Value is string @string && @string == "Bool")
                {
                    if (data.BoolData != null && boolIndex < data.BoolData.Length)
                    {
                        bool boolValue = data.BoolData[boolIndex];
                        data.Settings[kvp.Key] = boolValue;
                    }
                    boolIndex++;
                }
            }

            foreach (var opt in OptionItem.AllTBROptions)
            {
                if (data.Settings.TryGetValue(opt.Id, out var sync))
                {
                    opt.SyncValue(sync, false);
                    continue;
                }

                opt.SetToDefault();
                opt.SyncAUOption();
                opt.UpdateVisuals();
            }

            SettingsHudDisplay.Instance?.BuildPages();
        }
    }
}

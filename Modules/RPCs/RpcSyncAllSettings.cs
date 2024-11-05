using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

public enum SettingType
{
    Bool,
    Float,
    Int
}

namespace TheBetterRoles.RPCs
{
    [RegisterCustomRpc((uint)ReactorRPCs.SyncAllSettings)]
    public class RpcSyncAllSettings : PlayerCustomRpc<Main, RpcSyncAllSettings.Data>
    {
        public override SendOption SendOption => SendOption.Reliable;
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
        public RpcSyncAllSettings(Main plugin, uint id) : base(plugin, id)
        {
        }

        public readonly struct Data(Dictionary<int, string>? settings = null, byte[]? boolData = null)
        {
            public readonly Dictionary<int, string>? Settings = settings;
            public readonly byte[]? BoolData = boolData;
        }

        public override void Write(MessageWriter writer, Data data)
        {
            List<int> ids = [];
            int count = 0;

            // Main buffer for Float, Int, and Id data
            using (var buffer = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(buffer))
            {
                // Buffer for Bool values
                List<byte> boolBuffer = [];
                byte boolByte = 0;
                int boolIndex = 0;

                foreach (var item in BetterOptionItem.BetterOptionItems)
                {
                    if (ids.Contains(item.Id)) continue;

                    if (item is BetterOptionFloatItem floatItem && floatItem.CurrentValue != floatItem.defaultValue)
                    {
                        binaryWriter.Write((byte)SettingType.Float);
                        binaryWriter.Write(item.Id);
                        binaryWriter.Write((float)Math.Round(floatItem.CurrentValue, 5));
                        ids.Add(item.Id);
                        count++;
                    }
                    else if (item is BetterOptionIntItem intItem && intItem.CurrentValue != intItem.defaultValue)
                    {
                        binaryWriter.Write((byte)SettingType.Int);
                        binaryWriter.Write(item.Id);
                        binaryWriter.Write(intItem.CurrentValue);
                        ids.Add(item.Id);
                        count++;
                    }
                    else if (item is BetterOptionCheckboxItem checkboxItem && checkboxItem.IsChecked != checkboxItem.defaultValue)
                    {
                        binaryWriter.Write((byte)SettingType.Bool);
                        binaryWriter.Write(item.Id);
                        // Pack the boolean into the boolByte
                        if (checkboxItem.IsChecked)
                        {
                            boolByte |= (byte)(1 << boolIndex); // Set the bit for true
                        }
                        boolIndex++;

                        // If we've packed 8 booleans, store the byte and reset
                        if (boolIndex == 8)
                        {
                            boolBuffer.Add(boolByte);
                            boolByte = 0; // Reset for the next byte
                            boolIndex = 0;
                        }

                        ids.Add(item.Id);
                        count++;
                    }
                    else if (item is BetterOptionPercentItem percentItem && percentItem.CurrentValue != percentItem.defaultValue)
                    {
                        binaryWriter.Write((byte)SettingType.Float); // Percent treated as Float
                        binaryWriter.Write(item.Id);
                        binaryWriter.Write(percentItem.CurrentValue);
                        ids.Add(item.Id);
                        count++;
                    }
                    else if (item is BetterOptionStringItem stringItem && stringItem.CurrentValue != stringItem.defaultValue)
                    {
                        binaryWriter.Write((byte)SettingType.Int); // StringItem treated as Int
                        binaryWriter.Write(item.Id);
                        binaryWriter.Write(stringItem.CurrentValue);
                        ids.Add(item.Id);
                        count++;
                    }
                }

                // If there are any remaining booleans that weren't added
                if (boolIndex > 0)
                {
                    boolBuffer.Add(boolByte); // Add the last byte if it's not full
                }

                writer.Write(count);
                writer.Write(buffer.ToArray()); // Write main data buffer
                writer.Write(boolBuffer.Count);  // Write the number of packed Bool bytes
                writer.Write(boolBuffer.ToArray()); // Write the packed Bool buffer as separate data
            }
        }

        public override Data Read(MessageReader reader)
        {
            // Create a dictionary to store settings
            Dictionary<int, string> settings = new Dictionary<int, string>();

            // Read the number of settings
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
                        settings.Add(id, "Bool"); // Placeholder to identify this as a Bool
                        break;
                }
            }

            // Read the Bool data buffer
            int boolBufferLength = reader.ReadInt32();
            byte[] boolData = reader.ReadBytes(boolBufferLength);

            return new Data(settings, boolData);
        }

        public override void Handle(PlayerControl player, Data data)
        {
            if (player.IsHost())
            {
                BetterDataManager.HostSettings.Clear();
                GameSettingMenuPatch.SetupSettings(true, true);

                // Process Bool values from the Bool data buffer
                int boolByteCount = data.BoolData.Length;
                int boolIndex = 0;

                foreach (var kvp in data.Settings.ToList()) // Convert to list to allow modification
                {
                    if (kvp.Value == "Bool") // Check for Bool placeholder
                    {
                        int byteIndex = boolIndex / 8;
                        if (byteIndex < boolByteCount)
                        {
                            bool boolValue = (data.BoolData[byteIndex] & (1 << (boolIndex % 8))) != 0;
                            data.Settings[kvp.Key] = boolValue.ToString();
                        }
                        boolIndex++;
                    }
                }

                // Save settings
                foreach (var kvp in data.Settings)
                {
                    BetterDataManager.SaveSetting(kvp.Key, kvp.Value);
                    BetterOptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == kvp.Key)?.SyncValue(kvp.Value);
                }
            }
        }
    }
}

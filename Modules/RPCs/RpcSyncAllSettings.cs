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
        public RpcSyncAllSettings(Main plugin, uint id) : base(plugin, id)
        {
        }

        public override SendOption SendOption => SendOption.Reliable;

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

        public readonly struct Data(MessageReader? reader)
        {
            public readonly MessageReader? Reader = reader;
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
            return new Data(reader);
        }

        public override void Handle(PlayerControl player, Data data)
        {
            MessageReader? reader = data.Reader;

            if (reader != null)
            {
                if (player.IsHost())
                {
                    BetterDataManager.HostSettings.Clear();
                    GameSettingMenuPatch.SetupSettings(true);

                    int count = reader.ReadInt32(); // Read the number of settings

                    // First, read the main data buffer
                    Dictionary<int, string> settings = new Dictionary<int, string>();
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
                                // We only read the Id here, the actual Bool value will be read from the Bool buffer later
                                settings.Add(id, "Bool"); // Placeholder to identify this as a Bool
                                break;
                        }
                    }

                    // Next, read the Bool data buffer
                    int boolBufferLength = reader.ReadInt32();
                    byte[] boolData = reader.ReadBytes(boolBufferLength);

                    // Process Bool values
                    int boolByteCount = boolData.Length;
                    int boolIndex = 0;

                    foreach (var kvp in settings.ToList()) // Convert to list to allow modification
                    {
                        if (kvp.Value == "Bool") // Check for Bool placeholder
                        {
                            // Calculate which byte and bit to read
                            int byteIndex = boolIndex / 8;
                            if (byteIndex < boolByteCount)
                            {
                                bool boolValue = (boolData[byteIndex] & 1 << boolIndex % 8) != 0; // Check the specific bit
                                settings[kvp.Key] = boolValue.ToString();
                            }
                            boolIndex++;
                        }
                    }

                    // Save settings
                    foreach (var kvp in settings)
                    {
                        BetterDataManager.SaveSetting(kvp.Key, kvp.Value);
                        BetterOptionItem.BetterOptionItems?.FirstOrDefault(op => op.Id == kvp.Key)?.SyncValue(kvp.Value);
                    }
                }
            }
        }
    }
}

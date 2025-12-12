using Hazel;
using InnerNet;
using System.Globalization;
using TheBetterRoles.Items;
using TheBetterRoles.Modules.CustomSystems;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides utility methods for working with network communication, including reading/writing packed data,
/// managing player and object references, and performing RPC (Remote Procedure Call) operations.
/// This class also includes helper methods for spawning and despawning objects locally in a networked environment.
/// </summary>
internal static class HazelHelper
{
    /// <summary>
    /// Serializes a value using the most efficient method for its type.
    /// Supported types:
    /// - Primitives (int, bool, float, etc.)
    /// - Strings
    /// - Vector2 (compressed to 4 bytes)
    /// - Collections (bool[], byte[], IEnumerable<>)
    /// - Networked objects (via NetIDs)
    /// - ObjectHash
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown for null values</exception>
    /// <exception cref="NotSupportedException">Thrown for unsupported types</exception>
    internal static void WriteFast(this MessageWriter writer, object? value)
    {
        if (value == null)
        {
            writer.Write(true);
            return;
        }
        writer.Write(false);

        switch (value)
        {
            case int i: writer.WritePacked(i); break;
            case uint i: writer.WritePacked(i); break;
            case long i: writer.WritePacked(i); break;
            case ulong i: writer.WritePacked(i); break;
            case byte i: writer.Write(i); break;
            case float i: writer.Write(i); break;
            case sbyte i: writer.Write(i); break;
            case ushort i: writer.Write(i); break;
            case bool i: writer.Write(i); break;
            case string i: writer.Write(i); break;
            case Vector2 i: writer.WriteVector2(i); break;
            case IEnumerable<bool> i: writer.WriteBooleans(i); break;
            case IEnumerable<byte> i: writer.WritePackedBytes(i); break;
            case InnerNetObject i when i != null: writer.WritePacked(i.NetId); break;
            case NetworkClass i when i != null: writer.WritePacked(i.NetworkId); break;
            case NetworkClassMono i when i != null: writer.WritePacked(i.NetworkId); break;
            case ObjectHash i when i != null: writer.Write(i.Hash); break;
            case DeadBody i when i != null: writer.WriteDeadBody(i); break;
            case null: throw new ArgumentNullException(nameof(value));
            default: throw new NotSupportedException($"Couldn't serialize {value.GetType().Name}");
        }
    }

    /// <summary>
    /// Deserializes a value with type safety.
    /// Returns default(T) if:
    /// - The read network object ID resolves to null
    /// - The ObjectHash lookup fails
    /// </summary>
    internal static T ReadFast<T>(this MessageReader reader)
    {
        object? value = ReadFast(reader, typeof(T));

        if (value is T typedValue)
            return typedValue;

        throw new NullReferenceException($"{typeof(T).Name} is null");
    }

    /// <summary>
    /// Non-generic variant of ReadFast for dynamic type handling.
    /// Used when the target type isn't known at compile time.
    /// </summary>
    internal static object? ReadFast(this MessageReader reader, Type objectType)
    {
        if (objectType == null)
            throw new ArgumentNullException(nameof(objectType));

        var isNull = reader.ReadBoolean();
        if (isNull)
        {
            return null;
        }

        switch (Type.GetTypeCode(objectType))
        {
            case TypeCode.Int32: return reader.ReadPackedInt32();
            case TypeCode.UInt32: return reader.ReadPackedUInt32();
            case TypeCode.Int64: return reader.ReadPackedLong();
            case TypeCode.UInt64: return reader.ReadPackedULong();
            case TypeCode.Byte: return reader.ReadByte();
            case TypeCode.Single: return reader.ReadSingle();
            case TypeCode.SByte: return reader.ReadSByte();
            case TypeCode.UInt16: return reader.ReadUInt16();
            case TypeCode.Boolean: return reader.ReadBoolean();
            case TypeCode.String: return reader.ReadString();
        }

        if (objectType == typeof(Vector2))
            return reader.ReadVector2();
        if (objectType == typeof(IEnumerable<bool>))
            return reader.ReadBooleans().AsEnumerable();
        if (objectType == typeof(bool[]))
            return reader.ReadBooleans();
        if (objectType == typeof(List<bool>))
            return reader.ReadBooleans().ToList();
        if (objectType == typeof(HashSet<bool>))
            return reader.ReadBooleans().ToHashSet();

        if (typeof(IEnumerable<byte>).IsAssignableFrom(objectType))
            return reader.ReadPackedBytes().AsEnumerable();
        if (objectType == typeof(byte[]))
            return reader.ReadPackedBytes();
        if (objectType == typeof(List<byte>))
            return reader.ReadPackedBytes().ToList();
        if (objectType == typeof(HashSet<byte>))
            return reader.ReadPackedBytes().ToHashSet();

        if (typeof(InnerNetObject).IsAssignableFrom(objectType))
        {
            if (AmongUsClient.Instance.FindObjectByNetId<InnerNetObject>(reader.ReadPackedUInt32()) is InnerNetObject netObj)
                return netObj;
            throw new NullReferenceException($"{objectType.Name} is null");
        }
        else if (typeof(NetworkClass).IsAssignableFrom(objectType))
        {
            if (NetworkClass.GetFromNetId(reader.ReadPackedUInt32()) is NetworkClass netClass)
                return netClass;
            throw new NullReferenceException($"{objectType.Name} is null");
        }
        else if (typeof(NetworkClassMono).IsAssignableFrom(objectType))
        {
            if (NetworkClassMono.GetFromNetId(reader.ReadPackedUInt32()) is NetworkClassMono netClassMono)
                return netClassMono;
            throw new NullReferenceException($"{objectType.Name} is null");
        }
        else if (typeof(ObjectHash).IsAssignableFrom(objectType))
        {
            if (ObjectHashExtension.TryGetObjectByHash(reader.ReadUInt16()) is ObjectHash objHash)
                return objHash;
            throw new NullReferenceException($"{objectType.Name} is null");
        }
        else if (typeof(DeadBody).IsAssignableFrom(objectType))
        {
            if (reader.ReadDeadBody() is DeadBody deadBody)
                return deadBody;
            throw new NullReferenceException($"{objectType.Name} is null");
        }

        throw new NotSupportedException($"Couldn't deserialize {objectType.Name}");
    }

    /// <summary>
    /// Compresses a Vector2 to 4 bytes (2x UInt16) using normalized range mapping.
    /// Uses NetHelpers.X/YRange for coordinate space normalization.
    /// Precision: ~0.000015f (1/65535)
    /// </summary>
    internal static void WriteVector2(this MessageWriter writer, Vector2 vec)
    {
        writer.Write((ushort)(NetHelpers.XRange.ReverseLerp(vec.x) * 65535f));
        writer.Write((ushort)(NetHelpers.YRange.ReverseLerp(vec.y) * 65535f));
    }

    /// <summary>
    /// Decompresses a Vector2 from 4 bytes using range-aware interpolation.
    /// Inverse operation of WriteVector2.
    /// </summary>
    internal static Vector2 ReadVector2(this MessageReader reader)
    {
        return new Vector2(
            NetHelpers.XRange.Lerp(reader.ReadUInt16() / 65535f),
            NetHelpers.YRange.Lerp(reader.ReadUInt16() / 65535f)
            );
    }

    /// <summary>
    /// Writes an array of boolean values to a MessageWriter in a packed format to save space.
    /// Each byte stores up to 8 boolean values as bits.
    /// </summary>
    internal static void WriteBooleans(this MessageWriter writer, IEnumerable<bool> boolsEnumerable)
    {
        bool[] bools = boolsEnumerable.ToArray();

        writer.Write(bools.Length);

        byte currentByte = 0;
        int bitIndex = 0;

        foreach (bool b in bools)
        {
            if (b) currentByte |= (byte)(1 << bitIndex);

            bitIndex++;

            if (bitIndex == 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitIndex = 0;
            }
        }

        if (bitIndex > 0) writer.Write(currentByte);
    }

    /// <summary>
    /// Reads an array of boolean values from a MessageReader that were previously packed using WriteBooleans.
    /// </summary>
    internal static bool[] ReadBooleans(this MessageReader reader)
    {
        int length = reader.ReadInt32();
        bool[] bools = new bool[length];

        int bitIndex = 0;
        byte currentByte = 0;

        for (int i = 0; i < length; i++)
        {

            if (bitIndex == 0) currentByte = reader.ReadByte();

            bools[i] = (currentByte & (1 << bitIndex)) != 0;

            bitIndex++;

            if (bitIndex == 8) bitIndex = 0;
        }

        return bools;
    }

    /// <summary>
    /// Writes bytes with dynamic packing. Automatically detects and writes the max value.
    /// </summary>
    internal static void WritePackedBytes(this MessageWriter writer, IEnumerable<byte> bytes)
    {
        byte[] byteArray = bytes.ToArray();
        if (byteArray.Length == 0)
        {
            writer.Write((byte)0);
            return;
        }

        byte maxValue = byteArray.Max();

        int bitsPerValue = (int)Math.Log(maxValue, 2) + 1;
        int valuesPerByte = 8 / bitsPerValue;

        writer.Write(maxValue);
        writer.WritePacked(byteArray.Length);

        if (bitsPerValue >= 8 || valuesPerByte == 1)
        {
            writer.Write(byteArray);
            return;
        }

        byte currentByte = 0;
        int bitsFilled = 0;

        foreach (byte value in byteArray)
        {
            currentByte |= (byte)(value << bitsFilled);
            bitsFilled += bitsPerValue;

            if (bitsFilled >= 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitsFilled = 0;
            }
        }

        if (bitsFilled > 0)
            writer.Write(currentByte);
    }

    /// <summary>
    /// Reads dynamically packed bytes. Uses maxValue from the stream to unpack.
    /// </summary>
    internal static byte[] ReadPackedBytes(this MessageReader reader)
    {
        byte maxValue = reader.ReadByte();
        if (maxValue == 0) return [];

        int count = reader.ReadPackedInt32();
        byte[] result = new byte[count];

        int bitsPerValue = (int)Math.Log(maxValue, 2) + 1;
        int valuesPerByte = 8 / bitsPerValue;

        if (bitsPerValue >= 8 || valuesPerByte == 1)
        {
            return reader.ReadBytes(count);
        }

        byte mask = (byte)((1 << bitsPerValue) - 1);
        for (int i = 0; i < count; i++)
        {
            int byteIndex = i / valuesPerByte;
            int bitOffset = (i % valuesPerByte) * bitsPerValue;

            if (bitOffset == 0)
            {
                byte packedByte = reader.ReadByte();
                for (int j = 0; j < valuesPerByte; j++)
                {
                    int idx = byteIndex * valuesPerByte + j;
                    if (idx >= count) break;
                    result[idx] = (byte)((packedByte >> (j * bitsPerValue)) & mask);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Writes a packed 64-bit signed integer using variable-length encoding with ZigZag encoding.
    /// </summary>
    internal static void WritePacked(this MessageWriter writer, long value)
    {
        ulong zigzag = (ulong)((value << 1) ^ (value >> 63));
        writer.WritePacked(zigzag);
    }

    /// <summary>
    /// Reads a packed 64-bit signed integer that was written with WritePacked(long).
    /// </summary>
    internal static long ReadPackedLong(this MessageReader reader)
    {
        ulong zigzag = reader.ReadPackedULong();
        return (long)((zigzag >> 1) ^ (~(zigzag & 1) + 1));
    }

    /// <summary>
    /// Writes a packed 64-bit unsigned integer using variable-length encoding.
    /// </summary>
    internal static void WritePacked(this MessageWriter writer, ulong value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            writer.Write(b);
        } while (value != 0);
    }

    /// <summary>
    /// Reads a packed 64-bit unsigned integer that was written with WritePacked(ulong).
    /// </summary>
    internal static ulong ReadPackedULong(this MessageReader reader)
    {
        ulong result = 0;
        int shift = 0;
        byte b;
        do
        {
            b = reader.ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0 && shift < 64);

        return result;
    }

    /// <summary>
    /// Writes a floating-point value to a MessageWriter in a packed format to save space.
    /// Handles negative values, integers, and decimal numbers efficiently.
    /// </summary>
    internal static void WritePacked(this MessageWriter writer, float value)
    {
        bool isNegative = value < 0;
        float absValue = Math.Abs(value);

        // Check if it's a solid number (integer)
        bool isSolidNumber = absValue == (uint)absValue && absValue <= uint.MaxValue;

        // Track the number of decimal places dynamically
        int decimalPlaces = 0;
        int scaleFactor = 1;
        bool needsFallback = false; // Flag to determine if we need to send the value as a regular float

        // Check if the value is not a solid number
        if (!isSolidNumber)
        {
            // Convert to string to count decimals (avoiding floating point precision issues)
            string absValueStr = absValue.ToString("G", CultureInfo.InvariantCulture);
            if (absValueStr.Contains('.'))
            {
                decimalPlaces = absValueStr.Split('.')[1].Length; // Count decimals after the period
                scaleFactor = (int)Math.Pow(10, decimalPlaces); // Set scale factor dynamically
            }
        }

        // If the value can't be packed efficiently, we need a fallback
        if (!isSolidNumber || absValue * scaleFactor > uint.MaxValue)
        {
            needsFallback = true; // Flag for fallback as a regular float
        }

        // Pack the flags into a list of booleans
        var flags = new List<bool>
        {
            isNegative,
            isSolidNumber,
            decimalPlaces > 0,
            needsFallback
        };

        // Write the flags using WriteBooleans
        writer.WriteBooleans(flags);

        // Write the decimal places as a byte (0-3 for simplicity)
        writer.Write((byte)decimalPlaces);

        // If it's a solid number, pack as uint
        if (isSolidNumber && !needsFallback)
        {
            writer.Write((uint)absValue);
        }

        // If it's a decimal number, scale and pack it as uint (but check if it can fit)
        else if (decimalPlaces > 0 && !needsFallback)
        {
            // Check if the scaled value fits into a uint
            if (absValue * scaleFactor <= uint.MaxValue)
            {
                uint scaledValue = (uint)(absValue * scaleFactor); // Dynamically scale the value
                writer.Write(scaledValue);
            }
            else
            {
                // If it can't fit into a uint, send as regular float
                writer.Write(value);
            }
        }
        else
        {
            // Send as regular float if it requires fallback
            writer.Write(value);
        }
    }

    /// <summary>
    /// Reads a floating-point value from a MessageReader that was previously packed using WritePacked.
    /// Handles negative values, integers, and decimal numbers efficiently.
    /// </summary>
    internal static float ReadPackedSingle(this MessageReader reader)
    {
        var flags = reader.ReadBooleans();
        bool isNegative = flags[0];
        bool isSolidNumber = flags[1];
        bool hasDecimalPart = flags[2];
        bool needsFallback = flags[3];

        byte decimalPlaces = reader.ReadByte(); // Read the decimal places count

        // If the value was packed as a regular float, just read it (don't reapply the sign)
        if (needsFallback)
        {
            return reader.ReadSingle(); // The sign is already handled in the packed value
        }

        // Handle solid number (uint) case
        if (isSolidNumber)
        {
            uint packedValue = reader.ReadUInt32();
            return isNegative ? -(float)packedValue : (float)packedValue;
        }
        // Handle decimal number (scaled uint)
        else if (hasDecimalPart)
        {
            uint scaledValue = reader.ReadUInt32();
            float scaleFactor = (float)Math.Pow(10, decimalPlaces); // Use the stored decimal places count
            return isNegative ? -(scaledValue / scaleFactor) : (scaledValue / scaleFactor);
        }
        else
        {
            // For other cases where it's a normal float
            float value = reader.ReadSingle();
            return isNegative ? -value : value;
        }
    }

    /// <summary>
    /// Writes a PlayerControl object's NetId to a MessageWriter.
    /// If the player is null, writes uint.MaxValue.
    /// </summary>
    internal static void WritePlayer(this MessageWriter writer, PlayerControl player) => writer.Write(player?.NetId ?? uint.MaxValue);

    /// <summary>
    /// Reads a PlayerControl object's NetId from a MessageReader and returns the corresponding player.
    /// Returns null if the NetId is uint.MaxValue.
    /// </summary>
    internal static PlayerControl? ReadPlayer(this MessageReader reader) => AmongUsClient.Instance.FindObjectByNetId<PlayerControl>(reader.ReadUInt32());

    /// <summary>
    /// Writes a NetworkedPlayerInfo object's NetId to a MessageWriter.
    /// If the data is null, writes uint.MaxValue.
    /// </summary>
    internal static void WritePlayerData(this MessageWriter writer, NetworkedPlayerInfo data) => writer.Write(data?.NetId ?? uint.MaxValue);

    /// <summary>
    /// Reads a NetworkedPlayerInfo object's NetId from a MessageReader and returns the corresponding data.
    /// Returns null if the NetId is uint.MaxValue.
    /// </summary>
    internal static NetworkedPlayerInfo? ReadPlayerData(this MessageReader reader) => AmongUsClient.Instance.FindObjectByNetId<NetworkedPlayerInfo>(reader.ReadUInt32());

    /// <summary>
    /// Writes a DeadBody object's hash to a MessageWriter.
    /// If the body is null, writes ushort.MaxValue.
    /// </summary>
    internal static void WriteDeadBody(this MessageWriter writer, DeadBody body) => writer.Write(body?.GetObjHash() ?? ushort.MaxValue);

    /// <summary>
    /// Reads a DeadBody object's hash from a MessageReader and returns the corresponding body.
    /// Returns null if the hash is ushort.MaxValue.
    /// </summary>
    internal static DeadBody? ReadDeadBody(this MessageReader reader) => Main.AllDeadBodys.FirstOrDefault(deadbody => deadbody.GetObjHash() == reader.ReadUInt16());

    /// <summary>
    /// Writes a Vent object's ID to a MessageWriter in a packed format.
    /// If the vent is null, writes -1.
    /// </summary>
    internal static void WriteVent(this MessageWriter writer, Vent vent) => writer.WritePacked(vent?.Id ?? -1);

    /// <summary>
    /// Reads a Vent object's ID from a MessageReader and returns the corresponding vent.
    /// Returns null if the ID is -1.
    /// </summary>
    internal static Vent? ReadVent(this MessageReader reader) => VentFactorySystem.Instance?.AllVents?.FirstOrDefault(vent => vent.Id == reader.ReadPackedInt32());

    /// <summary>
    /// Converts a MessageWriter to a MessageReader, allowing the written data to be read.
    /// </summary>
    internal static MessageReader ToReader(this MessageWriter writer) => MessageReader.Get(writer.ToByteArray(false));

    /// <summary>
    /// Splits a MessageWriter into multiple MessageReaders by extracting individual messages from the stream.
    /// </summary>
    internal static MessageReader[] ToReaders(this MessageWriter writer)
    {
        var reader = writer.ToReader();
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessage());
        }

        return [.. readers];
    }

    /// <summary>
    /// Splits a MessageReader into multiple MessageReaders by extracting individual messages from the stream.
    /// </summary>
    internal static MessageReader[] ToReaders(this MessageReader reader)
    {
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessage());
        }

        return [.. readers];
    }

    /// <summary>
    /// Splits a MessageReader into multiple MessageReaders, creating new buffer instances for each extracted message.
    /// </summary>
    internal static MessageReader[] ToReadersNewBuffer(this MessageReader reader)
    {
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessageAsNewBuffer());
        }

        return [.. readers];
    }

    /// <summary>
    /// Writes multiple MessageReader instances into a MessageWriter as individual messages.
    /// Optionally clears the writer before writing.
    /// </summary>
    internal static MessageWriter WriteReaders(this MessageWriter writer, IEnumerable<MessageReader> readers, bool clear = false)
    {
        if (clear) writer.Clear(writer.SendOption);

        foreach (MessageReader reader in readers)
        {
            writer.StartMessage(reader.Tag);
            writer.Write(reader.ReadBytes(reader.Length));
            writer.EndMessage();
        }

        return writer;
    }

    /// <summary>
    /// Creates a copy of the MessageWriter, preserving its data and send options.
    /// </summary>
    internal static MessageWriter Copy(this MessageWriter writer)
    {
        var newWriter = MessageWriter.Get(writer.SendOption);
        newWriter.Write(writer.ToByteArray(false));
        return newWriter;
    }

    /// <summary>
    /// Starts an RPC (Remote Procedure Call) desync operation for specific clients.
    /// </summary>
    internal static List<MessageWriter> StartRpcDesync(this InnerNetClient client, uint playerNetId, byte callId, SendOption option, int[] ignoreClientIds, Func<ClientData, bool>? clientCheck = null)
    {
        List<MessageWriter> messageWriters = [];

        if (ignoreClientIds.All(Int => Int < 0))
        {
            messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, -1));
        }
        else
        {
            foreach (var allClients in AmongUsClient.Instance.allClients)
            {
                if (ignoreClientIds.Contains(allClients.Id)) continue;
                if (clientCheck == null || clientCheck.Invoke(allClients))
                {
                    messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, allClients.Id));
                }
            }
        }

        return messageWriters;
    }

    /// <summary>
    /// Finishes an RPC desync operation by sending the messages and recycling the writers.
    /// </summary>
    internal static void FinishRpcDesync(this InnerNetClient client, List<MessageWriter> messageWriters)
    {
        foreach (var msg in messageWriters)
        {
            msg.EndMessage();
            msg.EndMessage();
            client.SendOrDisconnect(msg);
            msg.Recycle();
        }
    }

    /// <summary>
    /// Spawns all InnerNetObjects in a GameObject locally.
    /// </summary>
    internal static void SpawnAllLocally(this GameObject obj, int ownerId = -2)
    {
        foreach (var netObj in obj.GetComponentsInChildren<InnerNetObject>())
        {
            netObj.SpawnLocally(ownerId);
        }
    }

    /// <summary>
    /// Spawns all InnerNetObjects in a MonoBehaviour's GameObject locally.
    /// </summary>
    internal static void SpawnAllLocally(this MonoBehaviour mono, int ownerId = -2) => mono.gameObject.SpawnAllLocally(ownerId);

    /// <summary>
    /// Spawns an InnerNetObject locally.
    /// </summary>
    internal static void SpawnLocally(this InnerNetObject obj, int ownerId = -2)
    {
        if (ownerId >= 0)
        {
            obj.OwnerId = ownerId;
        }
        InnerNetClient innerNetClient = AmongUsClient.Instance;
        if (innerNetClient != null)
        {
            obj.NetId = innerNetClient.NetIdCnt;
            innerNetClient.AddNetObject(obj);
        }
    }

    /// <summary>
    /// Despawns all InnerNetObjects in a GameObject locally.
    /// </summary>
    internal static void DespawnAllLocally(this GameObject obj)
    {
        foreach (var netObj in obj.GetComponentsInChildren<InnerNetObject>())
        {
            netObj.DespawnLocally();
        }
    }

    /// <summary>
    /// Despawns all InnerNetObjects in a MonoBehaviour's GameObject locally.
    /// </summary>
    internal static void DespawnAllLocally(this MonoBehaviour mono) => mono.gameObject.DespawnAllLocally();

    /// <summary>
    /// Despawns an InnerNetObject locally.
    /// </summary>
    internal static void DespawnLocally(this InnerNetObject obj)
    {
        InnerNetClient innerNetClient = AmongUsClient.Instance;
        if (innerNetClient != null)
        {
            innerNetClient.DestroyedObjects.Add(obj.NetId);
            innerNetClient.RemoveNetObject(obj);
        }
    }
}
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Data;

internal sealed class GameSettingsFile : AbstractJsonFile
{
    internal override string FilePath => TBRDataManager.SettingsFile;

    internal override bool Load()
    {
        Settings.Clear();
        var success = base.Load();
        if (success)
        {
            foreach (var kvp in Settings.ToArray())
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    try
                    {
                        Settings[kvp.Key] = jsonElement.ValueKind switch
                        {
                            JsonValueKind.Number when jsonElement.TryGetInt32(out int intValue) => intValue,
                            JsonValueKind.Number when jsonElement.TryGetSingle(out float floatValue) => floatValue,
                            JsonValueKind.Number when jsonElement.TryGetByte(out byte byteValue) => byteValue,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => jsonElement.GetString(),
                            _ => throw new NotSupportedException($"Unsupported JSON type: {jsonElement.ValueKind}")
                        };
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to convert JSON element for key {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }
        return success;
    }

    protected override void WriteToFile(string json)
    {
        var jsonDoc = JsonDocument.Parse(json);
        var settingsDict = jsonDoc.RootElement.GetProperty(nameof(Settings));
        var sb = new StringBuilder();

        foreach (var kvp in settingsDict.EnumerateObject())
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(kvp.Name).Append('/').Append(kvp.Value.GetRawText());
        }

        byte[] flattenedData = Encoding.UTF8.GetBytes(sb.ToString());
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(flattenedData, 0, flattenedData.Length);
        }
        ms.Position = 0;
        File.WriteAllText(FilePath, Convert.ToBase64String(ms.ToArray()));
    }

    protected override string ReadFromFile()
    {
        byte[] compressedBytes = Convert.FromBase64String(File.ReadAllText(FilePath).Trim());
        using var ms = new MemoryStream(compressedBytes);
        using var gzip = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        string flattenedData = reader.ReadToEnd();

        var settingsDict = new Dictionary<string, JsonElement>();
        foreach (var pair in flattenedData.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('/', 2);
            if (parts.Length == 2)
            {
                using var doc = JsonDocument.Parse(parts[1]);
                settingsDict[parts[0]] = doc.RootElement.Clone();
            }
        }

        var resultDict = new Dictionary<int, object?>();
        foreach (var kvp in settingsDict)
        {
            if (int.TryParse(kvp.Key, out int key))
            {
                resultDict[key] = kvp.Value.Deserialize<object?>();
            }
        }

        return JsonSerializer.Serialize(new { Settings = resultDict });
    }

    [JsonPropertyName(nameof(Settings))]
    public Dictionary<int, object?> Settings { get; set; } = [];
}

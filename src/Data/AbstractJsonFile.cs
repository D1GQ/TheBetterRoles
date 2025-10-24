using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Data;

internal abstract class AbstractJsonFile
{
    internal abstract string FilePath { get; }
    private bool _hasInit;

    protected virtual JsonSerializerOptions SerializerOptions { get; } = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true,
    };

    internal virtual void Init()
    {
        if (_hasInit) return;
        _hasInit = true;

        if (!CheckFile() || !Load())
        {
            Save();
        }
    }

    internal virtual bool Load()
    {
        try
        {
            var content = TryReadFromFile();
            if (string.IsNullOrEmpty(content.Trim()))
            {
                return false;
            }

            var data = JsonSerializer.Deserialize(content, GetType(), SerializerOptions);
            if (data == null)
            {
                Logger.Error("Deserialization returned null");
                return false;
            }

            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonPropertyNameAttribute>() == null) continue;
                if (property.CanWrite)
                {
                    var value = property.GetValue(data);
                    property.SetValue(this, value);
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            Logger.Error($"JSON parsing error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    private string TryReadFromFile()
    {
        try
        {
            return ReadFromFile();
        }
        catch
        {
            return string.Empty;
        }
    }

    protected virtual string ReadFromFile()
    {
        return File.ReadAllText(FilePath);
    }

    internal virtual bool Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, GetType(), SerializerOptions);
            WriteToFile(json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }

        return true;
    }

    protected virtual void WriteToFile(string json)
    {
        File.WriteAllText(FilePath, json);
    }

    private bool CheckFile()
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(FilePath))
        {
            return false;
        }

        var content = TryReadFromFile();
        if (string.IsNullOrEmpty(content.Trim())) return false;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);

        var fileInfo = new FileInfo(FilePath);
        if (fileInfo.Length == 0)
        {
            return false;
        }

        try
        {
            if (jsonElement.ValueKind == JsonValueKind.Object && !jsonElement.EnumerateObject().Any() ||
                jsonElement.ValueKind == JsonValueKind.Array && !jsonElement.EnumerateArray().Any())
            {
                return false;
            }
        }
        catch (JsonException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}

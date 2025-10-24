using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the data for a custom name plate, with properties for the name plate's package, ID, name, author, folder location, sprites for various states,
/// and settings for its appearance and behavior in the game.
/// </summary>
internal class CustomNamePlateData
{
    /// <summary>
    /// The package associated with the custom name plate.
    /// </summary>
    [JsonPropertyName("package")] public string Package { get; set; }

    /// <summary>
    /// The ID for the custom name plate.
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    /// The name of the custom name plate.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; }

    /// <summary>
    /// The author of the custom name plate.
    /// </summary>
    [JsonPropertyName("author")] public string Author { get; set; }

    /// <summary>
    /// The folder location for the custom name plate's assets.
    /// </summary>
    [JsonPropertyName("folder")] public string Folder { get; set; }

    /// <summary>
    /// The sprite for the custom name plate.
    /// </summary>
    [JsonPropertyName("sprite")] public string Sprite { get; set; }

    /// <summary>
    /// Deserializes a JSON file into a Customname plateData object from the specified file path.
    /// </summary>
    internal static CustomNamePlateData? Deserialize(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContent = File.ReadAllText(filePath);
        CustomNamePlateData newConfig = JsonSerializer.Deserialize<CustomNamePlateData>(fileContent, options);

        return newConfig;
    }
}
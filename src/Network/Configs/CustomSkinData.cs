using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the data for a custom skin, with properties for the skin's package, ID, name, author, folder location, sprites for various states,
/// and settings for its appearance and behavior in the game.
/// </summary>
internal class CustomSkinData
{
    /// <summary>
    /// The package associated with the custom skin.
    /// </summary>
    [JsonPropertyName("package")] public string Package { get; set; }

    /// <summary>
    /// The ID for the custom skin.
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    /// The name of the custom skin.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; }

    /// <summary>
    /// The author of the custom skin.
    /// </summary>
    [JsonPropertyName("author")] public string Author { get; set; }

    /// <summary>
    /// The folder location for the custom skin's assets.
    /// </summary>
    [JsonPropertyName("folder")] public string Folder { get; set; }

    /// <summary>
    /// The spritesheet for the custom skin.
    /// </summary>
    [JsonPropertyName("sprite")] public string Sprite { get; set; }

    /// <summary>
    /// The preview sprite for the custom skin.
    /// </summary>
    [JsonPropertyName("preview")] public string Preview { get; set; }

    /// <summary>
    /// The eject sprite for the custom skin.
    /// </summary>
    [JsonPropertyName("eject")] public string Eject { get; set; }

    /// <summary>
    /// The sprite for the custom skin.
    /// </summary>
    [JsonPropertyName("ref_skin_id")] public string RefSkinId { get; set; }

    /// <summary>
    /// Flag indicating whether the custom skin uses the player.
    /// </summary>
    [JsonPropertyName("colorbase")] public bool ColorBase { get; set; }

    /// <summary>
    /// Deserializes a JSON file into a CustomskinData object from the specified file path.
    /// </summary>
    internal static CustomSkinData? Deserialize(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContent = File.ReadAllText(filePath);
        CustomSkinData newConfig = JsonSerializer.Deserialize<CustomSkinData>(fileContent, options);

        return newConfig;
    }
}
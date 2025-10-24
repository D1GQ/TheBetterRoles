using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the data for a custom visor, with properties for the visor's package, ID, name, author, folder location, sprites for various states,
/// and settings for its appearance and behavior in the game.
/// </summary>
internal class CustomVisorData
{
    /// <summary>
    /// The package associated with the custom visor.
    /// </summary>
    [JsonPropertyName("package")] public string Package { get; set; }

    /// <summary>
    /// The ID for the custom visor.
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    /// The name of the custom visor.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; }

    /// <summary>
    /// The author of the custom visor.
    /// </summary>
    [JsonPropertyName("author")] public string Author { get; set; }

    /// <summary>
    /// The folder location for the custom visor's assets.
    /// </summary>
    [JsonPropertyName("folder")] public string Folder { get; set; }

    /// <summary>
    /// The sprite for the custom visor.
    /// </summary>
    [JsonPropertyName("sprite")] public string Sprite { get; set; }

    /// <summary>
    /// The sprite for the custom visor when flipped.
    /// </summary>
    [JsonPropertyName("flipsprite")] public string FlipSprite { get; set; }

    /// <summary>
    /// The sprite for the custom visor while climbing.
    /// </summary>
    [JsonPropertyName("climbsprite")] public string ClimbSprite { get; set; }

    /// <summary>
    /// Flag indicating whether the custom visor is placed behind hats.
    /// </summary>
    [JsonPropertyName("behindhats")] public bool BehindHats { get; set; }

    /// <summary>
    /// Flag indicating whether the custom visor uses the player.
    /// </summary>
    [JsonPropertyName("colorbase")] public bool ColorBase { get; set; }

    /// <summary>
    /// Deserializes a JSON file into a CustomVisorData object from the specified file path.
    /// </summary>
    internal static CustomVisorData? Deserialize(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContent = File.ReadAllText(filePath);
        CustomVisorData newConfig = JsonSerializer.Deserialize<CustomVisorData>(fileContent, options);

        return newConfig;
    }
}
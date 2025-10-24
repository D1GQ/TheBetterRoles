using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the data for a custom hat, with properties for the hat's package, ID, name, author, folder location, sprites for various states,
/// and settings for its appearance and behavior in the game.
/// </summary>
internal class CustomHatData
{
    /// <summary>
    /// The package associated with the custom hat.
    /// </summary>
    [JsonPropertyName("package")] public string Package { get; set; }

    /// <summary>
    /// The ID for the custom hat.
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    /// The name of the custom hat.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; }

    /// <summary>
    /// The author of the custom hat.
    /// </summary>
    [JsonPropertyName("author")] public string Author { get; set; }

    /// <summary>
    /// The folder location for the custom hat's assets.
    /// </summary>
    [JsonPropertyName("folder")] public string Folder { get; set; }

    /// <summary>
    /// The sprite for the custom hat.
    /// </summary>
    [JsonPropertyName("sprite")] public string Sprite { get; set; }

    /// <summary>
    /// The sprite for the custom hat when flipped.
    /// </summary>
    [JsonPropertyName("flipsprite")] public string FlipSprite { get; set; }

    /// <summary>
    /// The sprite for the custom hat when viewed from the back.
    /// </summary>
    [JsonPropertyName("backsprite")] public string BackSprite { get; set; }

    /// <summary>
    /// The sprite for the custom hat when flipped and viewed from the back.
    /// </summary>
    [JsonPropertyName("flipbacksprite")] public string FlipBackSprite { get; set; }

    /// <summary>
    /// The sprite for the custom hat while climbing.
    /// </summary>
    [JsonPropertyName("climbsprite")] public string ClimbSprite { get; set; }

    /// <summary>
    /// Flag indicating whether the custom hat is displayed behind the player.
    /// </summary>
    [JsonPropertyName("behind")] public bool Behind { get; set; }

    /// <summary>
    /// Flag indicating whether the custom hat has a bouncing effect.
    /// </summary>
    [JsonPropertyName("bounce")] public bool Bounce { get; set; }

    /// <summary>
    /// Flag indicating whether the custom hat uses the player.
    /// </summary>
    [JsonPropertyName("colorbase")] public bool ColorBase { get; set; }

    /// <summary>
    /// Deserializes a JSON file into a CustomHatData object from the specified file path.
    /// </summary>
    internal static CustomHatData? Deserialize(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContent = File.ReadAllText(filePath);
        CustomHatData newConfig = JsonSerializer.Deserialize<CustomHatData>(fileContent, options);

        return newConfig;
    }
}
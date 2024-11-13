using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Items;

public class CustomHatData
{
    [JsonPropertyName("package")] public string Package { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("author")] public string Author { get; set; }
    [JsonPropertyName("folder")] public string Folder { get; set; }
    [JsonPropertyName("sprite")] public string Sprite { get; set; }
    [JsonPropertyName("backsprite")] public string BackSprite { get; set; }
    [JsonPropertyName("climbsprite")] public string ClimbSprite { get; set; }
    [JsonPropertyName("behind")] public bool Behind { get; set; }
    [JsonPropertyName("bounce")] public bool Bounce { get; set; }
    [JsonPropertyName("colorbase")] public bool ColorBase { get; set; }

    public static CustomHatData? Serialize(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContent = File.ReadAllText(filePath);
        CustomHatData newConfig = JsonSerializer.Deserialize<CustomHatData>(fileContent, options);

        return newConfig;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheBetterRoles.Items
{
    public class CustomHat
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("folder")] public string Folder { get; set; }
        [JsonPropertyName("sprite")] public string Sprite { get; set; }
        [JsonPropertyName("backsprite")] public string BackSprite { get; set; }
        [JsonPropertyName("climbsprite")] public string ClimbSprite { get; set; }
        [JsonPropertyName("bounce")] public bool Bounce { get; set; }
        [JsonPropertyName("colorbase")] public bool ColorBase { get; set; }

        public static CustomHat? Serialize(string filePath)
        {
            var config = new CustomHat
            {
                Name = "Hat",
                Author = "None",
                Folder = "DefaultFolder",
                Sprite = "",
                BackSprite = "",
                ClimbSprite = "",
                Bounce = true,
                ColorBase = false
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(config, options);
            File.WriteAllText(filePath, jsonString);

            string fileContent = File.ReadAllText(filePath);
            CustomHat newConfig = JsonSerializer.Deserialize<CustomHat>(fileContent, options);

            return newConfig;
        }
    }
}

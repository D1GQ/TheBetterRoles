using System.Text.Json.Serialization;

namespace TheBetterRoles.Network.Configs;

/// <summary>
/// Represents the data structure for mod news items within the game.
/// Contains properties to store news-related details such as title, subtitle, content, and metadata.
/// </summary>
internal class NewsData()
{
    /// <summary>
    /// Indicates whether the news item should be shown.
    /// </summary>
    [JsonPropertyName("show")] public bool Show { get; set; }

    /// <summary>
    /// Specifies the language of the news item using an integer code.
    /// </summary>
    [JsonPropertyName("language")] public int Language { get; set; }

    /// <summary>
    /// Defines the type/category of the news item.
    /// </summary>
    [JsonPropertyName("type")] public int Type { get; set; }

    /// <summary>
    /// Unique identifier for the news item.
    /// </summary>
    [JsonPropertyName("id")] public uint Id { get; set; }

    /// <summary>
    /// The main title of the news item.
    /// </summary>
    [JsonPropertyName("title")] public string Title { get; set; }

    /// <summary>
    /// The subtitle providing additional context to the news title.
    /// </summary>
    [JsonPropertyName("subtitle")] public string SubTitle { get; set; }

    /// <summary>
    /// The title used for listing purposes.
    /// </summary>
    [JsonPropertyName("listtitle")] public string ListTitle { get; set; }

    /// <summary>
    /// The publication date of the news item, formatted as a string.
    /// </summary>
    [JsonPropertyName("date")] public string Date { get; set; }

    /// <summary>
    /// The content or body of the news item.
    /// </summary>
    [JsonPropertyName("content")] public string Content { get; set; }

    internal static NewsData? SanitizeYaml(string input)
    {
        var newsData = new NewsData();

        try
        {
            var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Trim leading and trailing spaces for each line
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("show:"))
                {
                    newsData.Show = bool.Parse(trimmedLine[6..].Trim());
                }
                else if (trimmedLine.StartsWith("language:"))
                {
                    newsData.Language = int.Parse(trimmedLine[9..].Trim());
                }
                else if (trimmedLine.StartsWith("type:"))
                {
                    newsData.Type = int.Parse(trimmedLine[5..].Trim());
                }
                else if (trimmedLine.StartsWith("id:"))
                {
                    newsData.Id = uint.Parse(trimmedLine[3..].Trim());
                }
                else if (trimmedLine.StartsWith("title:"))
                {
                    newsData.Title = trimmedLine[6..].Trim().Trim('"');
                }
                else if (trimmedLine.StartsWith("subtitle:"))
                {
                    newsData.SubTitle = trimmedLine[9..].Trim().Trim('"');
                }
                else if (trimmedLine.StartsWith("listtitle:"))
                {
                    newsData.ListTitle = trimmedLine[10..].Trim().Trim('"');
                }
                else if (trimmedLine.StartsWith("date:"))
                {
                    newsData.Date = trimmedLine[5..].Trim();
                }
                else if (trimmedLine.StartsWith("content:"))
                {
                    // Collect content lines until an empty line or end of string
                    var content = new List<string>();
                    int contentIndex = Array.IndexOf(lines, line) + 1;
                    while (contentIndex < lines.Length && !string.IsNullOrWhiteSpace(lines[contentIndex]))
                    {
                        content.Add(lines[contentIndex].Trim());
                        contentIndex++;
                    }
                    newsData.Content = string.Join("\n", content);
                }
            }

            return newsData;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to manually deserialize YAML: {ex.Message}");
            return null;
        }
    }
}
using AmongUs.Data;
using Assets.InnerNet;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles.Items;

internal class ModNews
{
    internal NewsTypes NewsType { get; }
    internal int Language { get; }
    internal int Number { get; }
    internal string Title { get; }
    internal string SubTitle { get; }
    internal string ShortTitle { get; }
    internal string Text { get; }
    internal string Date { get; }

    internal static List<NewsData> NewsDataToProcess { get; } = new();
    internal static List<ModNews> AllModNews { get; } = new();

    internal ModNews(NewsTypes type, int language, int number, string title, string subTitle, string shortTitle, string text, string date)
    {
        NewsType = type;
        Language = language;
        Number = number;
        Title = title;
        SubTitle = subTitle;
        ShortTitle = shortTitle;
        Text = text;
        Date = date;

        AllModNews.Add(this);
    }

    internal Announcement ToAnnouncement()
    {
        return new Announcement
        {
            Number = Number,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = (uint)DataManager.Settings.Language.CurrentLanguage,
            Date = Date,
            Id = "ModNews"
        };
    }

    internal static void ProcessModNewsFiles()
    {
        AllModNews.Clear();

        foreach (var config in NewsDataToProcess)
        {
            ParseModNewsContent(config);
        }
    }

    private static void ParseModNewsContent(NewsData config)
    {
        if (config.Id == 0) return;

        var type = (NewsTypes)config.Type;
        _ = new ModNews(type, config.Language, (int)config.Id, config.Title, config.SubTitle, config.ListTitle, config.Content, config.Date);
    }
}
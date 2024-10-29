using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

public enum NewsTypes
{
    None,
    TBR
}

[HarmonyPatch]
public class ModNews
{
    public NewsTypes NewsType;
    public int Number;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
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

        return result;
    }

    public static List<ModNews> AllModNews = new List<ModNews>();

    public ModNews(NewsTypes type, int number, string title, string subTitle, string shortTitle, string text, string date)
    {
        NewsType = type;
        Number = number;
        Title = title;
        SubTitle = subTitle;
        ShortTitle = shortTitle;
        Text = text;
        Date = date;
        AllModNews.Add(this);
    }

    private static string ReadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new FileNotFoundException("Resource not found: " + resourceName);

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    private static void ProcessModNewsFiles()
    {
        AllModNews.Clear();

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("BetterAmongUs.Resources.ModNews") && name.EndsWith(".txt"));

        foreach (var resourceName in resourceNames)
        {
            var content = ReadEmbeddedResource(resourceName);
            ParseModNewsContent(content);
        }
    }

    private static void ParseModNewsContent(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int number = 0;
        string title = "", subTitle = "", shortTitle = "", text = "", date = "";
        NewsTypes type = NewsTypes.None;

        foreach (var line in lines)
        {
            if (line.StartsWith("#Skip")) return;
            else if (line.StartsWith("#Type:"))
            {
                switch (line[6..])
                {
                    case "TBR":
                        type = NewsTypes.TBR;
                        break;
                }
            }
            else if (line.StartsWith("#Number:")) number = int.Parse(line[8..]);
            else if (line.StartsWith("#Title:")) title = line[7..];
            else if (line.StartsWith("#SubTitle:")) subTitle = line[10..];
            else if (line.StartsWith("#ShortTitle:")) shortTitle = line[12..];
            else if (line.StartsWith("#Date:")) date = line[6..];
            else if (line.StartsWith("#-----------------------------")) continue;
            else if (line.StartsWith("#") && line.Length <= 1) text += "\n";
            else text += line + "\n";
        }

        if (number != 0)
        {
            new ModNews(type, number, title, subTitle, shortTitle, text, date);
        }
    }

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
    public static void Initialize(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        static IEnumerator FetchBlacklist()
        {
            ProcessModNewsFiles();
            yield return null;
        }
        __result = Effects.Sequence(FetchBlacklist().WrapToIl2Cpp(), __result);
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        AllModNews.Sort((a1, a2) => DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));

        List<Announcement> finalAllNews = new List<Announcement>();
        AllModNews.ForEach(n => finalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!AllModNews.Any(x => x.Number == news.Number))
                finalAllNews.Add(news);
        }
        finalAllNews.Sort((a1, a2) => DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));

        aRange = new Il2CppReferenceArray<Announcement>(finalAllNews.Count);
        for (int i = 0; i < finalAllNews.Count; i++)
            aRange[i] = finalAllNews[i];

        return true;
    }

    [HarmonyPatch(typeof(AnnouncementPanel), nameof(AnnouncementPanel.SetUp)), HarmonyPostfix]
    public static void SetUpPanel(AnnouncementPanel __instance, [HarmonyArgument(0)] Announcement announcement)
    {
        if (announcement.Number < 100000) return;
        var obj = new GameObject("ModLabel");
        obj.transform.SetParent(__instance.transform);
        obj.transform.localPosition = new Vector3(-0.8f, 0.13f, 0.5f);
        obj.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        switch (AllModNews.Find(a => a.Number == announcement.Number).NewsType)
        {
            case NewsTypes.TBR:
                renderer.sprite = Utils.LoadSprite($"BetterAmongUs.Resources.Images.None.png", 650f);
                break;
            default:
                break;
        }
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
}

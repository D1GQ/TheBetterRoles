using System.Text;
using TheBetterRoles.Data;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class SettingsHudDisplay : MonoBehaviour
{
    internal static SettingsHudDisplay? Instance { get; private set; }

    private readonly List<(string Name, List<string> Pages)> Categories = [];
    private int currentCategory = 0;
    private int currentPage = 0;
    private TextMeshPro? SettingsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        var aspectPosition = gameObject.AddComponent<AspectPosition>();
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftTop;
        aspectPosition.DistanceFromEdge = new Vector3(10.1f, 2.6f, -5f);
        aspectPosition.AdjustPosition();

        SettingsText = gameObject.AddComponent<TextMeshPro>();
        SettingsText.fontMaterial = AmongUsClient.Instance.PlayerPrefab.cosmetics.nameText.fontMaterial;
        SettingsText.fontSize = 0.9f;
        SettingsText.alignment = TextAlignmentOptions.TopLeft;

        Instance = this;
    }

    private void Start()
    {
        if (Instance == null) return;

        BuildPages();
    }

    internal void BuildPages()
    {
        if (!GameState.IsLobby) return;

        int previousCategory = currentCategory;
        int previousPage = currentPage;
        Categories.Clear();

        static string NewLine(string str) => $"\n{Translator.GetString(str)}\n";

        var gameSettings = (Translator.GetString("Setting.Tab.GameSettings").ToColor(Color.green), new List<string>
        {
            NewLine("Setting.Title.PlayerSettings.Impostor") +
            $"{OptionItem.FormatOptionsToTextTrees([
                VanillaGameSettings.ImpostorVision,
                VanillaGameSettings.KillCooldown,
                VanillaGameSettings.KillDistance
                ], 100)}" +
            NewLine("Setting.Title.PlayerSettings.Crewmate") +
            $"{OptionItem.FormatOptionsToTextTrees([
                VanillaGameSettings.PlayerVision,
                VanillaGameSettings.PlayerSpeed,
                TBRGameSettings.ReportDistance
                ], 100)}" +
            NewLine("Setting.Title.MeetingSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                VanillaGameSettings.EmergencyMeetings,
                VanillaGameSettings.EmergencyCooldown,
                VanillaGameSettings.DiscussionTime,
                VanillaGameSettings.VotingTime,
                VanillaGameSettings.AnonymousVotes,
                VanillaGameSettings.ConfirmEjects
                ], 100)}" +
            NewLine("Setting.Title.TaskSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                VanillaGameSettings.TaskBarUpdate,
                VanillaGameSettings.CommonTasksNum,
                VanillaGameSettings.LongTasksNum,
                VanillaGameSettings.ShortTasksNum,
                VanillaGameSettings.VisualTask
                ], 100)}",

            NewLine("Setting.Title.MapSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                TBRGameSettings.ReverseSkeld,
                TBRGameSettings.ReverseMira,
                TBRGameSettings.ReversePolus,
                TBRGameSettings.ReverseAirship,
                TBRGameSettings.ReverseFungle,
                TBRGameSettings.BetterPolus
                ], 100)}" +
            NewLine("Setting.Title.SabotageSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                TBRGameSettings.CamouflageComms
                ], 100)}"
        });

        var systemSettings = (Translator.GetString("Setting.Tab.SystemSettings").ToColor(Color.yellow), new List<string>
        {
            NewLine("Setting.Title.ModSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                TBRGameSettings.Presets,
                TBRGameSettings.Algorithm,
                TBRGameSettings.NoGameEnd,
                TBRGameSettings.Debugging
                ], 100)}" +
            NewLine("Setting.Title.GuessSettings") +
            $"{OptionItem.FormatOptionsToTextTrees([
                TBRGameSettings.OnlyShowEnabledRoles,
                TBRGameSettings.CrewmatesCanGuess,
                TBRGameSettings.ImpostorsCanGuess,
                TBRGameSettings.BenignNeutralsCanGuess,
                TBRGameSettings.KillingNeutralsCanGuess,
                TBRGameSettings.CanGuessAddons
                ], 100)}"
        });

        Categories.Add(gameSettings);
        Categories.Add(systemSettings);

        var rolesByTeam = CustomRoleManager.RolePrefabs
            .Where(r => r.CanBeAssigned)
            .OrderBy(r => r.RoleTeam)
            .ThenBy(r => r.RoleCategory)
            .ThenBy(r => r.RoleName)
            .GroupBy(r => r.RoleTeam);

        var teamToCat = new Dictionary<RoleClassTeam, string>
        {
            { RoleClassTeam.Crewmate, Translator.GetString("Setting.Tab.CrewmateRoles").ToColor(Colors.CrewmateBlue) },
            { RoleClassTeam.Impostor, Translator.GetString("Setting.Tab.ImpostorRoles").ToColor(Colors.ImpostorRed) },
            { RoleClassTeam.Neutral, Translator.GetString("Setting.Tab.NeutralRoles").ToColor(Colors.NeutralGray) },
            { RoleClassTeam.Apocalypse, Translator.GetString("Setting.Tab.ApocalypseRoles").ToColor(Colors.ApocalypseGray) },
            { RoleClassTeam.None, Translator.GetString("Setting.Tab.Addons").ToColor(Color.magenta) }
        };

        foreach (var teamGroup in rolesByTeam)
        {
            var teamName = teamToCat[teamGroup.Key];
            var roles = teamGroup;
            var pages = new List<string>();
            var currentPage = new StringBuilder();
            int currentLineCount = 0;

            foreach (var role in roles)
            {
                role.RoleOptions.RoleOptionItem.FormatOptionsToTextTree();
                var roleOptions = role.RoleOptions.RoleOptionItem.FormatOptionsToTextTree(100f, false);
                var optionLines = roleOptions.Split('\n').Length;

                if (currentLineCount > 0 && currentLineCount + optionLines > 30)
                {
                    pages.Add(currentPage.ToString());
                    currentPage.Clear();
                    currentLineCount = 0;
                }

                currentPage.Append("\n");
                currentLineCount++;

                currentPage.Append(roleOptions);
                currentLineCount += optionLines;
            }

            if (currentPage.Length > 0)
            {
                pages.Add(currentPage.ToString());
            }

            if (pages.Count > 0)
            {
                Categories.Add((teamName, pages));
            }
        }

        if (Categories.Count > 0)
        {
            currentCategory = Math.Clamp(previousCategory, 0, Categories.Count - 1);
            currentPage = Math.Clamp(previousPage, 0, Categories[currentCategory].Pages.Count - 1);
        }
        else
        {
            currentCategory = 0;
            currentPage = 0;
        }

        UpdateDisplay();
    }

    internal void UpdateDisplay()
    {
        if (Categories.Count == 0 || SettingsText == null) return;

        var category = Categories[currentCategory];
        string pageContent = category.Pages.Count > 0
            ? category.Pages[currentPage]
            : "No settings available";

        SettingsText.SetText($"Tab ({currentCategory + 1}/{Categories.Count}): <b>{category.Name}</b>\n" +
                       $"Page ({currentPage + 1}/{category.Pages.Count})\n" +
                       $"{pageContent}\n" +
                       "<size=75%>Press [Tab] to view Next...</size>\n" +
                       "<size=75%>Press [Shift]+[Tab] to view Prev...</size>");
    }

    internal void SetCategory(int categoryIndex)
    {
        if (Categories.Count <= 0) return;

        currentCategory = categoryIndex % Categories.Count;
        currentPage = 0;
        UpdateDisplay();
    }

    internal void Next()
    {
        if (!GameState.IsLobby) return;
        if (Categories.Count == 0) return;
        var (_, Pages) = Categories[currentCategory];

        if (currentPage + 1 < Pages.Count)
        {
            currentPage++;
        }
        else
        {
            currentCategory = (currentCategory + 1) % Categories.Count;
            currentPage = 0;
        }

        UpdateDisplay();
    }

    internal void Previous()
    {
        if (!GameState.IsLobby) return;
        if (Categories.Count == 0) return;

        if (currentPage > 0)
        {
            currentPage--;
        }
        else
        {
            currentCategory = (currentCategory - 1 + Categories.Count) % Categories.Count;
            var (_, newPages) = Categories[currentCategory];
            currentPage = newPages.Count - 1;
        }

        UpdateDisplay();
    }
}
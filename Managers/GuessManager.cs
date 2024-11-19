using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using TheBetterRoles.RPCs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Managers;

public class GuessManager : MonoBehaviour
{
    public static GuessManager? Instance;
    public ShapeshifterMinigame? Minigame { get; set; }
    public Action<CustomRoleType, NetworkedPlayerInfo?> GuessAction = GuessPlayer;
    public PassiveButton? ButtonTemplate;
    private bool shouldSetButtons = true;

    public GuessTab? TabCrewmates;
    public GuessTab? TabImpostors;
    public GuessTab? TabNeutrals;
    public GuessTab? TabAddons;

    public int TargetId { get; set; } = -1;

    public void Start()
    {
        if (Instance != null && Instance != this || !GameState.IsMeeting)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Minigame = Instantiate(GamePrefabHelper.GetRolePrefab<ShapeshifterRole>(AmongUs.GameOptions.RoleTypes.Shapeshifter).ShapeshifterMenu);
        Minigame.name = "GuessManagerUI";
        Minigame.transform.SetParent(MeetingHud.Instance.transform, false);
        Minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
        var aspectSize = Minigame.GetComponent<AspectSize>();
        if (aspectSize != null)
        {
            aspectSize.PercentWidth = 0.85f;
            aspectSize.DoSetUp();
        }

        var Phone = Minigame.transform.Find("PhoneUI/Background").GetComponent<SpriteRenderer>();
        if (Phone != null)
        {
            PlayerMaterial.SetColors(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId, Phone.material);
        }
        var PhoneButton = Minigame.transform.Find("PhoneUI/UI_Phone_Button").GetComponent<SpriteRenderer>();
        if (PhoneButton != null)
        {
            PlayerMaterial.SetColors(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId, PhoneButton.material);
        }

        ButtonTemplate = Instantiate(MeetingHud.Instance.SkipVoteButton, Minigame.transform).GetComponent<PassiveButton>();
        ButtonTemplate.name = "Template";
        ButtonTemplate.gameObject.SetActive(true);
        ButtonTemplate.transform.position = Minigame.transform.position;
        ButtonTemplate.OnClick.RemoveAllListeners();
        var text = ButtonTemplate.GetComponentInChildren<TextMeshPro>();
        text.color = Color.white;
        text.outlineWidth = 0.1f;
        text.outlineColor = Color.black;
        ButtonTemplate.gameObject.SetActive(false);
        var pva = ButtonTemplate.GetComponent<PlayerVoteArea>();
        Destroy(ButtonTemplate.GetComponentInChildren<TextTranslatorTMP>());
        Destroy(pva.Buttons);
        Destroy(pva);
        ButtonTemplate.GetComponentInChildren<TextMeshPro>().text = "Text Here";

        if (GameState.IsMeeting)
        {
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(false);
        }

        CreateTabs();
        CreateRoles();
    }

    public PassiveButton CreateButton(string name,
        GameObject parent,
        AspectPosition.EdgeAlignments alignment = AspectPosition.EdgeAlignments.Center,
        Vector3? distanceFromEdge = null,
        Color? buttonColor = null,
        Color? textColor = null)
    {
        textColor ??= Color.black;
        buttonColor ??= Color.white;
        distanceFromEdge ??= Vector3.zero;

        var button = Instantiate(ButtonTemplate, parent.transform);
        button.gameObject.SetActive(true);
        button.name = $"Button({name})";
        var highlight = button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>();
        button.OnMouseOver.AddListener((Action)(() =>
        {
            button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>().enabled = true;
        }));
        button.OnMouseOut.AddListener((Action)(() =>
        {
            button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>().enabled = false;
        }));

        var aspect = button.gameObject.AddComponent<AspectPosition>();
        aspect.Alignment = alignment;
        aspect.DistanceFromEdge = (Vector3)distanceFromEdge;
        aspect.AdjustPosition();

        button.GetComponentInChildren<TextMeshPro>().text = name;
        highlight.color = (Color)buttonColor;
        button.GetComponent<SpriteRenderer>().color = (Color)buttonColor;
        var text = button.GetComponentInChildren<TextMeshPro>();
        text.color = (Color)textColor;

        return button;
    }

    private void CreateTabs()
    {
        GuessTab.AllTabs.Clear();

        var roles = CustomRoleManager.allRoles;

        if (roles.Where(r => r.IsCrewmate).Any())
            TabCrewmates = new GuessTab().Create(0, Translator.GetString("BetterSetting.Tab.CrewmateRoles"), this, CustomRoleTeam.Crewmate);
        if (roles.Where(r => r.IsImpostor).Any())
            TabImpostors = new GuessTab().Create(1, Translator.GetString("BetterSetting.Tab.ImpostorRoles"), this, CustomRoleTeam.Impostor);
        if (roles.Where(r => r.IsNeutral).Any())
            TabNeutrals = new GuessTab().Create(2, Translator.GetString("BetterSetting.Tab.NeutralRoles"), this, CustomRoleTeam.Neutral);
        if (roles.Where(r => r.IsAddon).Any() && TBRGameSettings.CanGuessAddons.GetBool())
            TabAddons = new GuessTab().Create(3, Translator.GetString("BetterSetting.Tab.Addons"), this, CustomRoleTeam.None);

        ChangeTab(0);
    }

    private void CreateRoles()
    {
        foreach (var role in CustomRoleManager.allRoles/*.OrderBy(r => r.GetType().Name)*/)
        {
            if (role.IsGhostRole) continue;
            if (!role.CanBeAssigned) continue;

            if (role.RoleType is
                CustomRoleType.Crewmate
                or CustomRoleType.Impostor)
            {
                goto skip;
            }

            if (role.GetChance() <= 0 && TBRGameSettings.OnlyShowEnabledRoles.GetBool())
            {
                continue;
            }

        skip:

            if (role.IsCrewmate)
            {
                TabCrewmates?.AddRole(role);
            }
            else if (role.IsImpostor)
            {
                TabImpostors?.AddRole(role);
            }
            else if (role.IsNeutral)
            {
                TabNeutrals?.AddRole(role);
            }
            else if (role.IsAddon)
            {
                TabAddons?.AddRole(role);
            }
        }
    }

    public void ChangeTab(int id)
    {
        TabCrewmates?.PagesRoot?.SetActive(false);
        TabImpostors?.PagesRoot?.SetActive(false);
        TabNeutrals?.PagesRoot?.SetActive(false);
        TabAddons?.PagesRoot?.SetActive(false);

        switch (id)
        {
            case 0:
                TabCrewmates?.PagesRoot?.SetActive(true);
                TabCrewmates?.UpdateButtons();
                break;
            case 1:
                TabImpostors?.PagesRoot?.SetActive(true);
                TabImpostors?.UpdateButtons();
                break;
            case 2:
                TabNeutrals?.PagesRoot?.SetActive(true);
                TabNeutrals?.UpdateButtons();
                break;
            case 3:
                TabAddons?.PagesRoot?.SetActive(true);
                TabAddons?.UpdateButtons();
                break;
        }
    }

    private static void GuessPlayer(CustomRoleType role, NetworkedPlayerInfo? targetData)
    {
        Instance.shouldSetButtons = false;
        var player = targetData.Object;
        if (player != null)
        {
            PlayerControl.LocalPlayer.SendRpcGuessPlayer(player, (int)role);
        }
    }

    public void FixedUpdate()
    {
        if (Minigame == null)
        {
            Destroy(this);
        }
    }

    public void OnDestroy()
    {
        if (GameState.IsMeeting && shouldSetButtons)
        {
            MeetingHud.Instance?.ButtonParent?.gameObject?.SetActive(true);
        }
    }
}


public class GuessTab
{
    public static List<GuessTab> AllTabs = [];
    public List<PassiveButton> allRoleButtons = [];
    public PassiveButton? TabButton;
    public TextMeshPro? TabText;
    public GameObject? TabRoot;
    public GameObject? PagesRoot;
    public List<GameObject> Pages = [];
    public int TabId { get; private set; }
    public string TabName { get; private set; } = string.Empty;
    private int RoleIndex { get; set; } = 0;
    private int pageButtonsIndex = 0;
    private int rolesPerPage = 42;
    private GuessManager? guessManager;
    private PassiveButton? nextPageButton;
    private PassiveButton? previousPageButton;

    public GuessTab Create(int id, string name, GuessManager guessManager, CustomRoleTeam team)
    {
        TabId = id;
        TabName = name;
        this.guessManager = guessManager;
        TabRoot = new GameObject($"Tab({name})");
        TabRoot.transform.SetParent(guessManager.Minigame.transform, false);
        PagesRoot = new GameObject($"Root");
        PagesRoot.transform.SetParent(TabRoot.transform, false);

        TabButton = guessManager.CreateButton(name, TabRoot, AspectPosition.EdgeAlignments.LeftTop, new Vector3(2.5f + 1.6f * AllTabs.Count, 0.8f, 0f), Utils.HexToColor32(Utils.GetCustomRoleTeamColor(team)));
        TabButton.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        TabButton.OnClick.AddListener((Action)(() =>
        {
            guessManager.ChangeTab(id);
        }));

        TabText = TabButton.GetComponentInChildren<TextMeshPro>();
        TabText.text = name;

        nextPageButton = guessManager.CreateButton("Next Page",
            PagesRoot,
            AspectPosition.EdgeAlignments.RightBottom,
            new Vector3(2f, 0.8f, 0f),
            null,
            null);
        nextPageButton.OnClick.AddListener((Action)(() =>
        {
            SwitchPage(true);
        }));

        previousPageButton = guessManager.CreateButton("Previous Page",
            PagesRoot,
            AspectPosition.EdgeAlignments.LeftBottom,
            new Vector3(2f, 0.8f, 0f),
            null,
            null);
        previousPageButton.OnClick.AddListener((Action)(() =>
        {
            SwitchPage(false);
        }));

        AllTabs.Add(this);

        CreateNewPage();
        Pages.First()?.SetActive(true);
        UpdateButtonState();
        return this;
    }

    private int pageIndex = 0;
    private void SwitchPage(bool next)
    {
        Pages[pageIndex].SetActive(false);

        if (next && pageIndex + 1 <= pageButtonsIndex)
        {
            pageIndex++;
        }
        else if (!next && pageIndex - 1 >= 0)
        {
            pageIndex--;
        }

        Pages[pageIndex].SetActive(true);
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        nextPageButton.enabled = false;
        previousPageButton.enabled = false;
        nextPageButton.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f);
        previousPageButton.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f);

        if (pageIndex > 0)
        {
            previousPageButton.enabled = true;
            previousPageButton.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else
        {
            previousPageButton.OnMouseOut.Invoke();
        }

        if (pageIndex < pageButtonsIndex)
        {
            nextPageButton.enabled = true;
            nextPageButton.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else
        {
            nextPageButton.OnMouseOut.Invoke();
        }

        UpdateButtons();
    }

    public void UpdateButtons()
    {
        foreach (var button in allRoleButtons)
        {
            button.GetComponentInChildren<TextMeshPro>().SetOutlineColor(new Color32(0, 0, 0, 255));
        }
    }

    public void AddRole(CustomRoleBehavior role)
    {
        // Check if the current page is full
        if (RoleIndex >= rolesPerPage)
        {
            RoleIndex = 0;
            CreateNewPage();
            pageButtonsIndex++;
        }

        var roleButton = guessManager.CreateButton(role.RoleName,
            Pages[pageButtonsIndex],
            AspectPosition.EdgeAlignments.Center,
            GetRoleButtonPos(),
            null,
            role.RoleColor32);
        roleButton.GetComponent<BoxCollider2D>().size = new Vector2(1.1f, 0.4f);
        roleButton.GetComponentInChildren<TextMeshPro>().SetOutlineColor(new Color32(0, 0, 0, 255));
        var spriteRenderer = roleButton.GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(1.1f, 0.4f);
        roleButton.transform.Find("ControllerHighlight").transform.localScale = new Vector3(1f, 1.4f, 1f);
        roleButton.OnClick.AddListener((Action)(() =>
        {
            if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.Results)
            {
                guessManager.GuessAction(role.RoleType, Utils.PlayerDataFromPlayerId(guessManager.TargetId));
            }
            guessManager.Minigame.Close();
        }));
        allRoleButtons.Add(roleButton);

        UpdateButtonState();
    }

    private void CreateNewPage()
    {
        var newPage = new GameObject($"Page({Pages.Count})");
        newPage.SetActive(false);
        newPage.transform.SetParent(PagesRoot.transform, false);
        Pages.Add(newPage);
    }

    private Vector3 GetRoleButtonPos()
    {
        var start = new Vector3(-3f, 1.5f, 0f); // Top-left starting point
        int columns = 6; // Number of columns
        float xOffset = 1.2f; // Horizontal distance between buttons
        float yOffset = 0.5f; // Vertical distance between rows

        int column = RoleIndex % columns;
        int row = RoleIndex / columns;

        RoleIndex++;

        float xPos = start.x + column * xOffset;
        float yPos = start.y - row * yOffset;

        return new Vector3(xPos, yPos, start.z);
    }

    public void Remove()
    {
        AllTabs.Remove(this);
    }
}
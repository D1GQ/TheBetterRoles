using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles.Core;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal class GuessManager : MonoBehaviour
{
    internal static GuessManager? Instance;
    internal ShapeshifterMinigame? Minigame { get; set; }
    internal Action<RoleClassTypes, NetworkedPlayerInfo?> GuessAction = GuessPlayer;
    internal PassiveButton? ButtonTemplate;

    internal GuessTab? TabCrewmates;
    internal GuessTab? TabImpostors;
    internal GuessTab? TabNeutrals;
    internal GuessTab? TabAddons;

    internal int TargetId { get; set; } = -1;
    private bool _shouldSetButtons = true;

    internal void Start()
    {
        if ((Instance != null && Instance != this) || !GameState.IsMeeting)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        InitializeUI();
        CreateTabs();
        CreateRoles();
    }

    private void InitializeUI()
    {
        Minigame = Instantiate(Prefab.GetCachedPrefab<ShapeshifterRole>().ShapeshifterMenu);
        Minigame.name = "GuessManagerUI";
        Minigame.transform.SetParent(MeetingHud.Instance.transform, false);
        Minigame.transform.localPosition = new Vector3(0f, 0f, -50f);

        SetupAspectSize();
        StylePhoneUI();
        CreateButtonTemplate();

        if (GameState.IsMeeting)
        {
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(false);
        }
    }

    private void SetupAspectSize()
    {
        var aspectSize = Minigame.GetComponent<AspectSize>();
        if (aspectSize == null) return;

        aspectSize.PercentWidth = 0.85f;
        aspectSize.DoSetUp();
    }

    private void StylePhoneUI()
    {
        StylePhoneElement("PhoneUI/Background");
        StylePhoneElement("PhoneUI/UI_Phone_Button");
    }

    private void StylePhoneElement(string path)
    {
        var element = Minigame.transform.Find(path)?.GetComponent<SpriteRenderer>();
        if (element == null) return;

        PlayerMaterial.SetColors(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId, element.material);
    }

    private void CreateButtonTemplate()
    {
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
    }

    internal PassiveButton CreateButton(string name, GameObject parent, AspectPosition.EdgeAlignments alignment = AspectPosition.EdgeAlignments.Center,
        Vector3? distanceFromEdge = null, Color? buttonColor = null, Color? textColor = null)
    {
        textColor ??= Color.black;
        buttonColor ??= Color.white;
        distanceFromEdge ??= Vector3.zero;

        var button = Instantiate(ButtonTemplate, parent.transform);
        button.gameObject.SetActive(true);
        button.name = $"Button({name})";
        button.transform.Find("Buttons")?.gameObject?.DestroyObj();

        SetupButtonHoverEffects(button);
        SetupButtonAppearance(button, name, (Color)buttonColor, (Color)textColor);
        SetupButtonPosition(button, alignment, (Vector3)distanceFromEdge);

        return button;
    }

    private static void SetupButtonHoverEffects(PassiveButton button)
    {
        var highlight = button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>();
        button.OnMouseOver.AddListener((Action)(() => highlight.enabled = true));
        button.OnMouseOut.AddListener((Action)(() => highlight.enabled = false));
    }

    private static void SetupButtonAppearance(PassiveButton button, string name, Color buttonColor, Color textColor)
    {
        button.GetComponentInChildren<TextMeshPro>().text = name;
        button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>().color = buttonColor;
        button.GetComponent<SpriteRenderer>().color = buttonColor;
        button.GetComponentInChildren<TextMeshPro>().color = textColor;
    }

    private static void SetupButtonPosition(PassiveButton button, AspectPosition.EdgeAlignments alignment, Vector3 distanceFromEdge)
    {
        var aspect = button.gameObject.AddComponent<AspectPosition>();
        aspect.Alignment = alignment;
        aspect.DistanceFromEdge = distanceFromEdge;
        aspect.AdjustPosition();
    }

    private void CreateTabs()
    {
        GuessTab.AllTabs.Clear();
        var roles = CustomRoleManager.RolePrefabs;

        if (roles.Any(r => r.IsCrewmate))
            TabCrewmates = new GuessTab().Create(0, Translator.GetString("Setting.Tab.CrewmateRoles"), this, RoleClassTeam.Crewmate);
        if (roles.Any(r => r.IsImpostor))
            TabImpostors = new GuessTab().Create(1, Translator.GetString("Setting.Tab.ImpostorRoles"), this, RoleClassTeam.Impostor);
        if (roles.Any(r => r.IsNeutral))
            TabNeutrals = new GuessTab().Create(2, Translator.GetString("Setting.Tab.NeutralRoles"), this, RoleClassTeam.Neutral);
        if (roles.Any(r => r.IsAddon) && TBRGameSettings.CanGuessAddons.GetBool())
            TabAddons = new GuessTab().Create(3, Translator.GetString("Setting.Tab.Addons"), this, RoleClassTeam.None);

        ChangeTab(0);
    }

    private void CreateRoles()
    {
        foreach (var role in CustomRoleManager.RolePrefabs)
        {
            if (ShouldSkipRole(role)) continue;

            if (role.IsCrewmate) TabCrewmates?.AddRole(role);
            else if (role.IsImpostor) TabImpostors?.AddRole(role);
            else if (role.IsNeutral) TabNeutrals?.AddRole(role);
            else if (role.IsAddon) TabAddons?.AddRole(role);
        }
    }

    private static bool ShouldSkipRole(RoleClass role)
    {
        if (role.RoleType is RoleClassTypes.Crewmate or RoleClassTypes.Impostor)
            return false;

        if (role.IsGhostRole) return true;
        if (!role.CanBeAssigned) return true;

        return !role.IsEnabled && TBRGameSettings.OnlyShowEnabledRoles.GetBool();
    }

    internal void ChangeTab(int id)
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

    private static void GuessPlayer(RoleClassTypes role, NetworkedPlayerInfo? targetData)
    {
        Instance._shouldSetButtons = false;
        var player = targetData?.Object;
        if (player != null)
        {
            PlayerControl.LocalPlayer.SendRpcGuessPlayer(player, role);
        }
    }

    internal void FixedUpdate()
    {
        if (Minigame == null)
        {
            Destroy(this);
        }
    }

    internal void OnDestroy()
    {
        if (GameState.IsMeeting && _shouldSetButtons)
        {
            MeetingHud.Instance?.ButtonParent?.gameObject?.SetActive(true);
        }
    }
}

internal class GuessTab
{
    internal static List<GuessTab> AllTabs = [];
    internal List<PassiveButton> AllRoleButtons = [];
    internal PassiveButton? TabButton;
    internal TextMeshPro? TabText;
    internal GameObject? TabRoot;
    internal GameObject? PagesRoot;
    internal List<GameObject> Pages = [];

    internal int TabId { get; private set; }
    internal string TabName { get; private set; } = string.Empty;

    private const int RolesPerPage = 42;
    private int _roleIndex;
    private int _pageButtonsIndex;
    private int _currentPageIndex;
    private GuessManager? _guessManager;
    private PassiveButton? _nextPageButton;
    private PassiveButton? _previousPageButton;

    internal GuessTab Create(int id, string name, GuessManager guessManager, RoleClassTeam team)
    {
        TabId = id;
        TabName = name;
        _guessManager = guessManager;

        CreateTabRoot(team);
        CreateNavigationButtons();

        AllTabs.Add(this);
        CreateNewPage();
        Pages.First()?.SetActive(true);
        UpdateButtonState();

        return this;
    }

    private void CreateTabRoot(RoleClassTeam team)
    {
        TabRoot = new GameObject($"Tab({TabName})");
        TabRoot.transform.SetParent(_guessManager.Minigame.transform, false);

        PagesRoot = new GameObject("Root");
        PagesRoot.transform.SetParent(TabRoot.transform, false);

        CreateTabButton(team);
    }

    private void CreateTabButton(RoleClassTeam team)
    {
        TabButton = _guessManager.CreateButton(
            TabName,
            TabRoot,
            AspectPosition.EdgeAlignments.LeftTop,
            new Vector3(2.5f + 1.6f * AllTabs.Count, 0.8f, 0f),
            Colors.HexToColor(Utils.GetCustomRoleTeamColorHex(team)));

        TabButton.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        TabButton.OnClick.AddListener((Action)(() => _guessManager.ChangeTab(TabId)));
        TabText = TabButton.GetComponentInChildren<TextMeshPro>();
    }

    private void CreateNavigationButtons()
    {
        _nextPageButton = CreateNavigationButton("Next Page", AspectPosition.EdgeAlignments.RightBottom, () => SwitchPage(true));
        _previousPageButton = CreateNavigationButton("Previous Page", AspectPosition.EdgeAlignments.LeftBottom, () => SwitchPage(false));
    }

    private PassiveButton CreateNavigationButton(string text, AspectPosition.EdgeAlignments alignment, Action onClick)
    {
        var button = _guessManager.CreateButton(
            text,
            PagesRoot,
            alignment,
            new Vector3(2f, 0.8f, 0f));

        button.OnClick.AddListener(onClick);
        return button;
    }

    private void SwitchPage(bool next)
    {
        Pages[_currentPageIndex].SetActive(false);

        if (next && _currentPageIndex + 1 <= _pageButtonsIndex)
        {
            _currentPageIndex++;
        }
        else if (!next && _currentPageIndex - 1 >= 0)
        {
            _currentPageIndex--;
        }

        Pages[_currentPageIndex].SetActive(true);
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        SetNavigationButtonState(_nextPageButton, _currentPageIndex < _pageButtonsIndex);
        SetNavigationButtonState(_previousPageButton, _currentPageIndex > 0);
        UpdateButtons();
    }

    private static void SetNavigationButtonState(PassiveButton button, bool enabled)
    {
        button.enabled = enabled;
        button.GetComponent<SpriteRenderer>().color = enabled ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        if (!enabled) button.OnMouseOut.Invoke();
    }

    internal void UpdateButtons()
    {
        foreach (var button in AllRoleButtons)
        {
            button.GetComponentInChildren<TextMeshPro>().SetOutlineColor(new Color32(0, 0, 0, 255));
        }
    }

    internal void AddRole(RoleClass role)
    {
        if (_roleIndex >= RolesPerPage)
        {
            _roleIndex = 0;
            CreateNewPage();
            _pageButtonsIndex++;
        }

        var roleButton = CreateRoleButton(role);
        AllRoleButtons.Add(roleButton);
        UpdateButtonState();
    }

    private PassiveButton CreateRoleButton(RoleClass role)
    {
        var roleButton = _guessManager.CreateButton(
            role.RoleName,
            Pages[_pageButtonsIndex],
            AspectPosition.EdgeAlignments.Center,
            GetRoleButtonPos(),
            null,
            role.RoleColor);

        ConfigureRoleButtonAppearance(roleButton);
        ConfigureRoleButtonBehavior(roleButton, role);

        return roleButton;
    }

    private static void ConfigureRoleButtonAppearance(PassiveButton button)
    {
        var collider = button.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.1f, 0.4f);

        var spriteRenderer = button.GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(1.1f, 0.4f);

        button.transform.Find("ControllerHighlight").transform.localScale = new Vector3(1f, 1.4f, 1f);
        button.GetComponentInChildren<TextMeshPro>().SetOutlineColor(new Color32(0, 0, 0, 255));
    }

    private void ConfigureRoleButtonBehavior(PassiveButton button, RoleClass role)
    {
        button.OnClick.AddListener((Action)(() =>
        {
            if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.Results)
            {
                _guessManager.GuessAction(role.RoleType, Utils.PlayerDataFromPlayerId(_guessManager.TargetId));
            }
            _guessManager.Minigame.Close();
        }));
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
        const float startX = -3f;
        const float startY = 1.5f;
        const int columns = 6;
        const float xOffset = 1.2f;
        const float yOffset = 0.5f;

        int column = _roleIndex % columns;
        int row = _roleIndex / columns;
        _roleIndex++;

        return new Vector3(
            startX + column * xOffset,
            startY - row * yOffset,
            0f);
    }

    internal void Remove()
    {
        AllTabs.Remove(this);
    }
}
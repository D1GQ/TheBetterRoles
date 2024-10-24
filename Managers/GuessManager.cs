using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TheBetterRoles;

public class GuessManager : MonoBehaviour
{
    public static GuessManager? Instance;
    public ShapeshifterMinigame? Minigame { get; set; }
    public Action<CustomRoles, NetworkedPlayerInfo?> GuessAction = GuessPlayer;
    public PassiveButton? ButtonTemplate;

    public GuessTab? TabCrewmates;
    public GuessTab? TabImpostors;
    public GuessTab? TabNeutrals;
    public GuessTab? TabAddons;

    public int TargetId { get; set; } = -1;

    public void Start()
    {
        if (Instance != null && Instance != this || !GameStates.IsMeeting)
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
        ButtonTemplate.gameObject.SetActive(true);
        ButtonTemplate.transform.position = Minigame.transform.position;
        ButtonTemplate.OnClick.RemoveAllListeners();
        ButtonTemplate.OnMouseOver.AddListener((Action)(() => 
        {
            ButtonTemplate.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>().enabled = true;
        }));
        ButtonTemplate.OnMouseOut.AddListener((Action)(() =>
        {
            ButtonTemplate.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>().enabled = false;
        }));
        ButtonTemplate.gameObject.SetActive(false);

        var pva = ButtonTemplate.GetComponent<PlayerVoteArea>();
        Destroy(ButtonTemplate.GetComponentInChildren<TextTranslatorTMP>());
        Destroy(pva.Buttons);
        Destroy(pva);
        ButtonTemplate.GetComponentInChildren<TextMeshPro>().text = "Text Here";

        if (GameStates.IsMeeting)
        {
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(false);
        }
    }

    public void ChangeTab(int id)
    {
        TabCrewmates?.Root?.SetActive(false);
        TabImpostors?.Root?.SetActive(false);
        TabNeutrals?.Root?.SetActive(false);
        TabAddons?.Root?.SetActive(false);

        switch (id)
        {
            case 0:
                TabCrewmates?.Root?.SetActive(true);
                break;
            case 1:
                TabImpostors?.Root?.SetActive(true);
                break;
            case 2:
                TabNeutrals?.Root?.SetActive(true);
                break;
            case 3:
                TabAddons?.Root?.SetActive(true);
                break;
        }
    }

    private static void GuessPlayer(CustomRoles role, NetworkedPlayerInfo? targetData)
    {
        var player = targetData.Object;
        if (player != null)
        {
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
        if (GameStates.IsMeeting)
        {
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(true);
        }
    }
}

public class GuessTab
{
    public static List<GuessTab> AllTabs = [];
    public GameObject? Root;
    public int TabId { get; private set; }
    public string TabName { get; private set; } = string.Empty;
    private GuessManager? guessManager;

    public GuessTab Create(int id, string name, GuessManager guessManager)
    {
        TabId = id;
        TabName = name;
        this.guessManager = guessManager;
        Root = new GameObject($"Tab({name})");
        Root.transform.SetParent(guessManager.transform, false);
        Root.SetActive(false);

        AllTabs.Add(this);
        return this;
    }

    public void AddRole(CustomRoleBehavior role)
    {

    }

    public void Remove()
    {
        AllTabs.Remove(this);
    }
}
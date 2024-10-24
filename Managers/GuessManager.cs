using UnityEngine;

namespace TheBetterRoles;

public class GuessManager : MonoBehaviour
{
    public static GuessManager? instance;
    public ShapeshifterMinigame? Minigame { get; set; }
    public int TargetId { get; set; } = -1;

    public void Start()
    {
        if (instance != null && instance != this || !GameStates.IsMeeting)
        {
            Destroy(this);
            return;
        }

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

        if (GameStates.IsMeeting)
        {
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(false);
        }

        instance = this;
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
using UnityEngine;

namespace TheBetterRoles;

public class PlayerMeetingButton
{
    public static List<PlayerMeetingButton> AllButtons = [];
    public Dictionary<PassiveButton, PlayerVoteArea> Buttons = [];
    public Action<PassiveButton?, PlayerVoteArea?, NetworkedPlayerInfo?> ClickAction = (PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData) => { };
    public Func<PlayerVoteArea, NetworkedPlayerInfo?, bool> ShowCondition = (pva, targetData) => { return !targetData.IsDead && !targetData.Disconnected && !targetData.IsLocalData(); };
    public CustomRoleBehavior? Role;
    public bool CanUseAsDead = false;
    public bool Enabled = true;

    public PlayerMeetingButton? Create(string name, CustomRoleBehavior? role = null, Sprite? sprite = null)
    {
        if (!GameStates.IsMeeting) return null;

        Role = role;
        Color color = Role != null ? Role.RoleColor32 : Color.white;

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            var Template = pva.ConfirmButton;
            var Button = UnityEngine.Object.Instantiate(Template, pva.transform).GetComponent<PassiveButton>();
            Buttons[Button] = pva;
            Button.transform.position -= new Vector3(1.8f, 0.1f, 2f);
            Button.transform.position += new Vector3(0.35f * AllButtons.Count, 0f, 0f);
            Button.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            Button.name = $"Button({name})";
            Button.OnClick = new();
            var target = Utils.PlayerDataFromPlayerId(pva.TargetPlayerId);
            Button.OnClick.AddListener((Action)(() => 
            {
                if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.Results)
                {
                    ClickAction.Invoke(Button, pva, target);
                }
            }));

            Button.GetComponent<SpriteRenderer>().sprite = sprite ?? HudManager.Instance.UseButton.graphic.sprite;
            var highlight = Button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>();
            if (highlight != null)
            {
                highlight.sprite = sprite ?? HudManager.Instance.UseButton.graphic.sprite;
                highlight.color = color;
            }
        }

        AllButtons.Add(this);
        return this;
    }

    public void Update()
    {
        foreach (var kvp in Buttons)
        {
            if (kvp.Value == null || kvp.Key == null || kvp.Key.gameObject == null)
                continue;

            var target = Utils.PlayerDataFromPlayerId(kvp.Value.TargetPlayerId);
            kvp.Key.gameObject.SetActive(ShowCondition(kvp.Value, target) && (PlayerControl.LocalPlayer.IsAlive() || CanUseAsDead) && Enabled);
        }
    }

    public void Remove()
    {
        AllButtons.Remove(this);
        foreach (var button in Buttons.Keys)
        {
            button.DestroyObj();
        }
    }
}
using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

internal class PlayerVoteAreaButton : MonoBehaviour
{
    internal static List<PlayerVoteAreaButton> AllButtons = [];
    internal List<(PassiveButton, PlayerVoteArea)> Buttons = [];
    internal Action<PassiveButton?, PlayerVoteArea?, NetworkedPlayerInfo?> ClickAction = (button, pva, targetData) => { };
    internal Func<PlayerVoteArea, NetworkedPlayerInfo?, bool> ShowCondition = (pva, targetData) => { return !targetData.IsDead && !targetData.Disconnected && !targetData.IsLocalData(); };
    internal RoleClass? Role;
    internal Sprite? Icon;
    internal bool CanUseAsDead { get; set; } = false;
    internal bool Enabled = true;

    /// <summary>
    /// Update button states, this is done instead of through update to reduce lag in meetings.
    /// </summary>
    internal static void UpdateAllButtonStates()
    {
        foreach (var button in AllButtons)
        {
            if (button == null) continue;
            button.UpdateButtonStates();
        }
    }

    /// <summary>
    /// Create ability button for all players in meeting.
    /// </summary>
    /// <param name="name">Name of ability button.</param>
    /// <param name="role">Role for ability button, this is used to determine color.</param>
    /// <param name="sprite">Sprite for ability.</param>
    /// <returns></returns>
    [HideFromIl2Cpp]
    internal PlayerVoteAreaButton? Create(string name, RoleClass? role = null, Sprite? sprite = null)
    {
        if (!GameState.IsMeeting) return null;
        PlayerVoteAreaButton button = MeetingHud.Instance.gameObject.AddComponent<PlayerVoteAreaButton>();

        button.Role = role;
        button.Icon = sprite;
        Color color = button.Role != null ? button.Role.RoleColor : Color.white;

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            var Template = pva.ConfirmButton;
            var Button = Instantiate(Template, pva.transform).GetComponent<PassiveButton>();
            button.Buttons.Add((Button, pva));
            Button.transform.position -= new Vector3(1.8f, 0.1f, 2f);
            Button.transform.position += new Vector3(0.35f * AllButtons.Count, 0f, 0f);
            Button.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            Button.name = $"Button({name})";
            var target = Utils.PlayerDataFromPlayerId(pva.TargetPlayerId);
            Button.OnClick = new();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.Results)
                {
                    button.ClickAction.Invoke(Button, pva, target);
                }
            }));

            Button.GetComponent<SpriteRenderer>().sprite = button.Icon ?? HudManagerPatch.catchedUseSprite;
            var highlight = Button.transform.Find("ControllerHighlight").GetComponent<SpriteRenderer>();
            if (highlight != null)
            {
                highlight.sprite = button.Icon ?? HudManagerPatch.catchedUseSprite;
                highlight.color = color;
            }
        }

        UpdateButtonStates();
        AllButtons.Add(button);
        return button;
    }

    internal void UpdateButtonStates()
    {
        foreach (var items in Buttons)
        {
            if (items.Item2 == null || items.Item1 == null || items.Item1.gameObject == null)
                continue;

            var target = Utils.PlayerDataFromPlayerId(items.Item2.TargetPlayerId);
            items.Item1.gameObject.SetActive(ShowCondition(items.Item2, target) && (PlayerControl.LocalPlayer.IsAlive() || CanUseAsDead) && Enabled);
        }
    }

    /// <summary>
    /// Remove button from meeting.
    /// </summary>
    internal void Remove()
    {
        AllButtons.Remove(this);
        foreach (var button in Buttons)
        {
            button.Item1.DestroyObj();
        }
        Destroy(this);
    }
}
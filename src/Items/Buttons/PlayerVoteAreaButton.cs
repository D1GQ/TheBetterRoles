using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

/// <summary>
/// Represents a button associated with a player's vote area during a meeting.
/// </summary>
internal class PlayerVoteAreaButton : MonoBehaviour
{
    internal static List<PlayerVoteAreaButton> AllButtons = [];
    internal List<(PassiveButton, PlayerVoteArea)> Buttons = [];
    internal Action<PassiveButton?, PlayerVoteArea?, NetworkedPlayerInfo?> ClickAction = (button, pva, targetData) => { };
    internal Func<PlayerVoteArea, NetworkedPlayerInfo?, bool> ShowCondition = (pva, targetData) =>
        !targetData.IsDead && !targetData.Disconnected && !targetData.IsLocalData();
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
            button?.UpdateButtonStates();
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
    internal static PlayerVoteAreaButton? Create(string name, RoleClass? role = null, Sprite? sprite = null)
    {
        if (!GameState.IsMeeting) return null;

        var button = MeetingHud.Instance.gameObject.AddComponent<PlayerVoteAreaButton>();
        button.Role = role;
        button.Icon = sprite;
        var color = button.Role?.RoleColor ?? Color.white;

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            var template = pva.ConfirmButton;
            var buttonInstance = Instantiate(template, pva.transform).GetComponent<PassiveButton>();
            button.Buttons.Add((buttonInstance, pva));

            buttonInstance.transform.position -= new Vector3(1.8f, 0.1f, 2f);
            buttonInstance.transform.position += new Vector3(0.35f * AllButtons.Count, 0f, 0f);
            buttonInstance.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            buttonInstance.name = $"Button({name})";

            var target = Utils.PlayerDataFromPlayerId(pva.TargetPlayerId);
            buttonInstance.OnClick = new();
            buttonInstance.OnClick.AddListener((Action)(() =>
            {
                if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted
                    or MeetingHud.VoteStates.Voted
                    or MeetingHud.VoteStates.Results)
                {
                    button.ClickAction.Invoke(buttonInstance, pva, target);
                }
            }));

            buttonInstance.GetComponent<SpriteRenderer>().sprite = button.Icon ?? HudManagerPatch.catchedUseSprite;
            var highlight = buttonInstance.transform.Find("ControllerHighlight")?.GetComponent<SpriteRenderer>();
            if (highlight != null)
            {
                highlight.sprite = button.Icon ?? HudManagerPatch.catchedUseSprite;
                highlight.color = color;
            }
        }

        button.UpdateButtonStates();
        AllButtons.Add(button);
        return button;
    }

    internal void UpdateButtonStates()
    {
        foreach (var (button, playerVoteArea) in Buttons)
        {
            if (playerVoteArea == null || button == null || button.gameObject == null) continue;

            var target = Utils.PlayerDataFromPlayerId(playerVoteArea.TargetPlayerId);
            button.gameObject.SetActive(
                ShowCondition(playerVoteArea, target) &&
                (PlayerControl.LocalPlayer.IsAlive() || CanUseAsDead) &&
                Enabled);
        }
    }

    /// <summary>
    /// Remove button from meeting.
    /// </summary>
    internal void Remove()
    {
        AllButtons.Remove(this);
        foreach (var (button, _) in Buttons)
        {
            button?.DestroyObj();
        }
        Destroy(this);
    }
}
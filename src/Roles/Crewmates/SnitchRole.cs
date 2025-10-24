using Cpp2IL.Core.Extensions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class SnitchRole : CrewmateRoleTBR, IRoleTaskAction, IRoleGuessAction
{
    internal sealed override int RoleId => 13;
    internal sealed override bool TaskReliantRole => true;
    internal sealed override string RoleColorHex => "#F3CE35";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Snitch;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Information;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? RevealRolesInMeeting;
    internal OptionItem? SnitchSeesNeutralRoles;
    internal OptionItem? TasksRemainingWhenRevealed;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RevealRolesInMeeting = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Snitch.Option.RevealRolesInMeeting", true, RoleOptions.RoleOptionItem),
                SnitchSeesNeutralRoles = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Snitch.Option.SnitchSeesNeutralRoles", false, RoleOptions.RoleOptionItem),
                TasksRemainingWhenRevealed = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Snitch.Option.TasksRemainingWhenRevealed", (0, 4, 1), 2, ("", ""), RoleOptions.RoleOptionItem)
            ];
        }
    }

    private readonly List<ArrowLocator> arrows = [];
    private bool HasFinishedTask = false;
    private bool HasHitTaskReveal = false;
    internal sealed override void OnSetUpRole()
    {
        TryOverrideTasks();
    }

    internal sealed override void OnDeinitialize()
    {
        var arrowsToRemove = arrows.Clone();
        foreach (var arrow in arrowsToRemove)
        {
            arrow.Remove();
        }
    }

    void IRoleTaskAction.TaskComplete(PlayerControl player, uint taskId)
    {
        if (_player.myTasks.ToArray().All(task => task.IsComplete))
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.Is(RoleClassTeam.Crewmate)) continue;
                if (target.Is(RoleClassTeam.Neutral) && !SnitchSeesNeutralRoles.GetBool()) continue;

                var arrow = new ArrowLocator().Create(color: target.GetTeamColor());
                arrow.SetTarget(target.gameObject);
                arrow.RemoveListener = () => { return target == null || !target.IsAlive(); };
                arrows.Add(arrow);
            }
            HasFinishedTask = true;
        }
    }

    void IRoleTaskAction.TaskCompleteOther(PlayerControl player, uint taskId)
    {
        if (!HasHitTaskReveal)
        {
            if (_player.myTasks.ToArray().Where(task => !task.IsComplete).Count() <= TasksRemainingWhenRevealed.GetInt())
            {
                RevealSelf();
                HasHitTaskReveal = true;
            }
        }
    }

    private void RevealSelf()
    {
        if (_player.IsLocalPlayer()) return;
        if (localPlayer.Is(RoleClassTeam.Crewmate)) return;
        if (localPlayer.Is(RoleClassTeam.Neutral) && !SnitchSeesNeutralRoles.GetBool()) return;

        Utils.FlashScreen("snitch", RoleColorHex);
        var arrow = new ArrowLocator().Create(color: RoleColor);
        arrow.SetTarget(_player.gameObject);
        arrow.RemoveListener = () => { return _player == null || !_player.IsAlive(); };
        arrows.Add(arrow);
    }

    bool IRoleGuessAction.CheckGuess(PlayerControl guesser, PlayerControl target, RoleClassTypes role)
    {
        if (target == _player)
        {
            if (HasFinishedTask)
            {
                if (guesser.IsLocalPlayer())
                {
                    HudManager.Instance.ShowPopUp(Translator.GetString("GuestManager.UnableToGuess", [target.GetPlayerNameAndColor()]));
                }

                return false;
            }
        }

        return true;
    }

    internal sealed override bool RevealPlayerRole(PlayerControl target)
    {
        if (HasFinishedTask)
        {
            if (target.Is(RoleClassTeam.Crewmate)) return false;
            if (target.Is(RoleClassTeam.Neutral) && !SnitchSeesNeutralRoles.GetBool()) return false;

            return true;
        }
        return false;
    }
}

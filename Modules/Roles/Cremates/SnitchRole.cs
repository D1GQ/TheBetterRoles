
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class SnitchRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 13;
    public override bool TaskReliantRole => true;
    public override string RoleColor => "#F3CE35";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Snitch;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Information;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public TBROptionItem? RevealRolesInMeeting;
    public TBROptionItem? SnitchSeesNeutralRoles;
    public TBROptionItem? TasksRemainingWhenRevealed;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RevealRolesInMeeting = new TBROptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Snitch.Option.RevealRolesInMeeting"), true, RoleOptionItem),
                SnitchSeesNeutralRoles = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Snitch.Option.SnitchSeesNeutralRoles"), false, RoleOptionItem),
                TasksRemainingWhenRevealed = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Snitch.Option.TasksRemainingWhenRevealed"), [0, 4, 1], 2, "", "", RoleOptionItem)
            ];
        }
    }

    private List<ArrowLocator> arrows = [];
    private bool HasFinishedTask = false;
    private bool HasHitTaskReveal = false;

    public override void OnSetUpRole()
    {
        TryOverrideTasks();
    }

    public override void OnDeinitialize()
    {
        List<ArrowLocator> arrowsToRemove = [];
        foreach (var arrow in arrows)
        {
            arrowsToRemove.Add(arrow);
        }
        foreach (var arrow in arrowsToRemove)
        {
            arrow.Remove();
        }
    }

    public override void OnTaskComplete(PlayerControl player, uint taskId)
    {
        if (_player.myTasks.ToArray().All(task => task.IsComplete))
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.Role().IsCrewmate) continue;
                if (target.Role().IsNeutral && !SnitchSeesNeutralRoles.GetBool()) continue;

                var arrow = new ArrowLocator().Create(color: target.GetTeamColor());
                arrow.SetPlayer(target);
                arrows.Add(arrow);
            }
            HasFinishedTask = true;
        }
    }

    public override void OnTaskCompleteOther(PlayerControl player, uint taskId)
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
        if (localPlayer.Role().IsCrewmate) return;
        if (localPlayer.Role().IsNeutral && !SnitchSeesNeutralRoles.GetBool()) return;

        Utils.FlashScreen(RoleColor);
        var arrow = new ArrowLocator().Create(color: RoleColor32);
        arrow.SetPlayer(_player);
        arrows.Add(arrow);
    }

    public override bool CheckGuess(PlayerControl guesser, PlayerControl target, CustomRoleType role)
    {
        if (target == _player)
        {
            if (HasFinishedTask)
            {
                if (guesser.IsLocalPlayer())
                {
                    DestroyableSingleton<HudManager>.Instance.ShowPopUp($"{target.Data.PlayerName} is unable to be guessed!");
                }

                return false;
            }
        }

        return true;
    }

    public override bool RevealPlayerRole(PlayerControl target)
    {
        if (HasFinishedTask)
        {
            if (target.Role().IsCrewmate) return false;
            if (target.Role().IsNeutral && !SnitchSeesNeutralRoles.GetBool()) return false;

            return true;
        }
        return false;
    }
}

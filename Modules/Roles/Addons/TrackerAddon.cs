using TheBetterRoles.Items;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class TrackerAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 29;
    public override string RoleColor => "#80FF00";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Tracker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
    private ArrowLocator? TrackerArrowLocator;
    public override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer()) TrackerArrowLocator = new ArrowLocator().Create(color: RoleColor32, maxScale: 0.5f, minDistance: 0f);
    }
    public override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer()) TrackerArrowLocator.Remove();
    }
    public override void FixedUpdate()
    {
        if (TrackerArrowLocator != null)
        {
            PlayerControl? target = null;
            float minDistance = float.MaxValue;
            var playerPosition = _player.GetTruePosition();

            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (_player == player || player == null) continue;

                float distance = Vector2.Distance(player.GetTruePosition(), playerPosition);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = player;
                }
            }

            if (target != null)
            {
                TrackerArrowLocator.Arrow.target = target.GetTruePosition() + new Vector2(0f, 0.25f);
                TrackerArrowLocator.Arrow.gameObject.SetActive(true);
            }
            else
            {
                TrackerArrowLocator.Arrow.gameObject.SetActive(false);
            }
        }
    }
}

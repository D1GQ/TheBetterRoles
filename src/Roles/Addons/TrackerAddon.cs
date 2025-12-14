using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class TrackerAddon : AddonClass, IRoleUpdateAction
{
    internal sealed override int RoleId => 29;
    internal sealed override string RoleColorHex => "#80FF00";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Tracker;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HelpfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private ArrowLocator? TrackerArrowLocator;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            TrackerArrowLocator = ArrowLocator.Create(color: RoleColor, maxScale: 0.5f, minDistance: 0f);
        }
    }
    internal sealed override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer()) TrackerArrowLocator?.Remove();
    }
    void IRoleUpdateAction.Update()
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

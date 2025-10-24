using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class NoiseMakerAddon : AddonClass, IRoleMurderAction
{
    internal sealed override int RoleId => 27;
    internal sealed override string RoleColorHex => "#920086";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.NoiseMaker;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HelpfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal OptionItem? ArrowDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ArrowDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.NoiseMaker.Option.ArrowDuration", (5f, 15f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    void IRoleMurderAction.Murder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            PlayNoiseNotification(ArrowDuration.GetFloat());
        }
    }

    private void PlayNoiseNotification(float duration)
    {
        var role = Prefab.GetCachedPrefab<NoisemakerRole>();
        if (Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.PlaySound(role.deathSound, false);
            VibrationManager.Vibrate(1f, PlayerControl.LocalPlayer.GetTruePosition(), 7f, 1.2f, VibrationManager.VibrationFalloff.None, null, false);
        }
        GameObject gameObject = UnityEngine.Object.Instantiate(role.deathArrowPrefab, _player.transform.position, Quaternion.identity);
        role.deathArrow = gameObject.GetComponent<NoisemakerArrow>();
        role.deathArrow.SetDuration(duration);
        if (_player.IsLocalPlayer())
        {
            role.deathArrow.alwaysMaxSize = true;
        }
        role.deathArrow.gameObject.SetActive(true);
        role.deathArrow.target = _player.GetTruePosition();
    }
}

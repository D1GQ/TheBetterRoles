using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class NoiseMakerAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#920086";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.NoiseMaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public BetterOptionItem? ArrowDuration;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ArrowDuration = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.NoiseMaker.Option.ArrowDuration"), [5f, 15f, 2.5f], 10f, "", "s", RoleOptionItem),
            ];
        }
    }

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            PlayNoiseNotification(ArrowDuration.GetFloat());
        }
    }

    private void PlayNoiseNotification(float duration)
    {
        var role = GamePrefabHelper.GetRolePrefab<NoisemakerRole>(AmongUs.GameOptions.RoleTypes.Noisemaker);
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

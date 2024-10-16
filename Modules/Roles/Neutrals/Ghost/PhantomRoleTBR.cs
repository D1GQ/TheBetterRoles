
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class PhantomRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override bool IsGhostRole => true;
    public override bool TaskReliantRole => true;
    public override bool HasSelfTask => !HasBeenClicked;
    public override string RoleColor => "#A04D8A";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Phantom;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Ghost;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;
    public override bool CanVent => _player.IsInVent();
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
    public float Alpha = 0f;
    public bool HasBeenClicked = false;
    public override void OnSetUpRole()
    {
        VentButton.VisibleCondition = _player.IsInVent;
        _player.Revive();
        _player.cosmetics.gameObject.SetActive(false);
        _player.cosmetics.colorBlindText.gameObject.SetActive(false);
        _player.cosmetics.CurrentPet?.gameObject.SetActive(false);
        InteractableTarget = false;
        _player.transform.Find("Names").gameObject.SetActive(false);
        TryOverrideTasks();
        SpawnInRandomVent();
    }

    public override void OnDeinitialize()
    {
        ResetState();
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        SpawnInRandomVent();
    }

    private void SpawnInRandomVent()
    {
        if (HasBeenClicked) return;
        var vent = Main.AllVents[UnityEngine.Random.Range(0, Main.AllVents.Count())];

        if (vent != null)
        {
            _player.transform.Find("Names").gameObject.SetActive(false);
            _player.cosmetics.SetPhantomRoleAlpha(0f);
            _player.NetTransform.SnapTo(vent.transform.position);
            _player.inVent = true;
            _player.Visible = false;
            _player.moveable = false;
            VentilationSystem.Update(VentilationSystem.Operation.Move, vent.Id);
            if (_player.IsLocalPlayer())
            {
                vent.SetButtons(true);
            }
        }
    }

    private void ResetState()
    {
        InteractableTarget = true;
        if (_player.IsLocalPlayer()) DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(_player.IsAlive());
        _player.transform.Find("Names").gameObject.SetActive(true);
        _player.cosmetics.SetPhantomRoleAlpha(1f);
        _player.cosmetics.gameObject.SetActive(true);
        _player.cosmetics.colorBlindText.gameObject.SetActive(true);
        _player.cosmetics.CurrentPet?.gameObject.SetActive(true);
    }

    public override void Update()
    {
        if (!HasBeenClicked)
        {
            if (_player.IsLocalPlayer()) DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(false);

            if (_player.MyPhysics.Animations.IsPlayingRunAnimation())
            {
                Alpha += 0.005f;
            }
            else
            {
                Alpha -= 0.0038f;
            }

            Alpha = Math.Clamp(Alpha, 0f, 0.20f);

            _player.cosmetics.SetPhantomRoleAlpha(Alpha);
        }
    }

    public override void OnPlayerPress(PlayerControl player, PlayerControl target)
    {
        if (Alpha > 0f && target == _player && player != _player)
        {
            _player.Exiled();
            ResetState();
            HasBeenClicked = true;
        }
    }

    public override bool WinCondition() => _player.Data.Tasks.ToArray().All(task => task.Complete) && _player.Data.Tasks.ToArray().Length > 0;
}

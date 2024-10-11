
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class PhantomRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override bool TaskReliantRole => true;
    public override bool HasSelfTask => !HasBeenClicked;
    public override string RoleColor => "#AB00BF";
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
        _player.Revive();
        _player.cosmetics.gameObject.SetActive(false);
        InteractableTarget = false;
        SetNameTextAlpha(0f);
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
            SetNameTextAlpha(0f);
            _player.cosmetics.SetPhantomRoleAlpha(0f);
            vent.SetButtons(true);
            _player.NetTransform.SnapTo(vent.transform.position);
            _player.inVent = true;
            _player.Visible = false;
            _player.moveable = false;
            VentilationSystem.Update(VentilationSystem.Operation.Move, vent.Id);
        }
    }

    private void ResetState()
    {
        InteractableTarget = true;
        DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(_player.IsAlive());
        _player.cosmetics.gameObject.SetActive(true);
        SetNameTextAlpha(1f);
        _player.cosmetics.SetPhantomRoleAlpha(1f);
    }

    public override void Update()
    {
        DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(false);
        if (!HasBeenClicked)
        {
            if (_player.MyPhysics.Animations.IsPlayingRunAnimation())
            {
                Alpha += 0.0025f;
            }
            else
            {
                Alpha -= 0.005f;
            }

            Alpha = Math.Clamp(Alpha, 0f, 0.20f);

            _player.cosmetics.SetPhantomRoleAlpha(Alpha);
        }
    }

    public override void OnPlayerPress(PlayerControl player, PlayerControl target)
    {
        if (Alpha > 0f && target == _player && player != _player)
        {
            ResetState();
            if (_player.IsLocalPlayer())
            {
                _player.MurderSync(_player, true, false, false, false, false);
            }
            HasBeenClicked = true;
        }
    }

    private void SetNameTextAlpha(float alpha)
    {
        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>())
        {
            text.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    public override bool WinCondition() => _player.Data.Tasks.ToArray().All(task => task.Complete);
}

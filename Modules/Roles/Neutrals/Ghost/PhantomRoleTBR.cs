
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class PhantomRoleTBR : CustomGhostRoleBehavior
{
    // Role Info
    public override int RoleId => 30;
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
        InteractableTarget = false;
        VentButton.VisibleCondition = _player.IsInVent;
        VentButton.UseAsDead = true;
        _player.BetterData().IsFakeAlive = true;
        _player.ClearAddons();
        _player.BetterData().PlayerVisionModPlus += 10;
        _player.Data.IsDead = true;
        _player.CustomRevive(false);
        _player.cosmetics.SetPhantomRoleAlpha(0f);
        _player.cosmetics.gameObject.SetActive(false);
        _player.transform.Find("Names").gameObject.SetActive(false);
        TryOverrideTasks(true);
        SpawnInRandomVent();
    }

    private void OnClick()
    {
        HasBeenClicked = true;
        _player.Visible = false;
        _player.Exiled();
        InteractableTarget = true;
        _player.BetterData().IsFakeAlive = false;
        _player.Data.IsDead = true;
        _player.cosmetics.gameObject.SetActive(true);
        _player.cosmetics.SetPhantomRoleAlpha(1f);
        _player.transform.Find("Names").gameObject.SetActive(true);
        if (_player.IsLocalPlayer()) _player.ShieldBreakAnimation(RoleColor);
    }

    public override void OnDeinitialize()
    {
        _player.BetterData().PlayerVisionModPlus -= 10;
        OnClick();
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (!HasBeenClicked)
        {
            SpawnInRandomVent();
        }
    }

    private void SpawnInRandomVent()
    {
        if (HasBeenClicked) return;
        var vent = _player.IsLocalPlayer() ? Main.AllEnabledVents[IRandom.Instance.Next(0, Main.AllEnabledVents.Length)] : Main.AllEnabledVents.First();

        if (vent != null)
        {
            _player.inVent = true;
            _player.Visible = false;
            _player.moveable = false;
            if (_player.IsLocalPlayer())
            {
                vent.TryMoveToVent(vent, out string _);
                vent.SetButtons(false);
            }
        }
    }

    public override void Update()
    {
        if (!HasBeenClicked)
        {
            _player.Visible = true;
        }
    }

    public override void FixedUpdate()
    {
        if (!HasBeenClicked)
        {
            if (_player.MyPhysics.Animations.IsPlayingRunAnimation() || _player.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || _player.inMovingPlat)
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
            if (player.IsLocalPlayer() && player.IsAlive())
            {
                OnClick();
                SendRoleSync(0);
            }
        }
    }

    public override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    OnClick();
                }
                break;
        }
    }

    public override bool WinCondition() => _player.Data.Tasks.ToArray().All(task => task.Complete) && _player.Data.Tasks.ToArray().Length > 0;

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsPhantomPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPrefix]
        public static void HandleAnimation_Prefix(PlayerPhysics __instance, ref bool amDead)
        {
            var player = __instance.myPlayer;
            if (!player.Is(CustomRoles.Phantom)) return;

            amDead = player?.IsAlive(true) == false;
        }
    }
}


using Hazel;
using UnityEngine;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class PlaguebearerRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#97BD3D";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Plaguebearer;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Chaos;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? playAnimation;
    public BetterOptionItem? InfectCooldown;
    public BetterOptionItem? InfectDistance;
    public BetterOptionItem? PestilenceKillCooldown;
    public TargetButton? InfectButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                playAnimation = new BetterOptionCheckboxItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.PlayAnimation"), false, RoleOptionItem),
                InfectCooldown = new BetterOptionFloatItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.InfectCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
                InfectDistance = new BetterOptionStringItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.InfectDistance"),
                    [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),
                PestilenceKillCooldown = new BetterOptionFloatItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Plaguebearer.Option.PestilenceKillCooldown"), [0f, 180f, 2.5f], 25, "", "s", RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        InfectButton = AddButton(new TargetButton().Create(5, Translator.GetString("Role.Plaguebearer.Ability.1"), InfectCooldown.GetFloat(), 0, null, this, true, InfectDistance.GetValue()));
        InfectButton.TargetCondition = (PlayerControl target) =>
        {
            return !Infected.Contains(target.Data);
        };
    }

    private List<NetworkedPlayerInfo> Infected = [];
    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        if (role != this) return;

        switch (id)
        {
            case 5:
                {
                    if (target != null)
                    {
                        InfectPlayer(target);
                    }
                }
                break;
        }
    }

    public override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var data in Infected)
            {
                var player = data.Object;
                if (player != null)
                {
                    player.SetTrueVisorColor(Palette.VisorColor);
                    player.BetterData().NameColor = string.Empty;
                }
            }
        }
    }

    // Infact player on interactions
    public override void OnPlayerInteractedOther(PlayerControl player, PlayerControl target)
    {
        if (target == _player && !Infected.Contains(player.Data))
        {
            InfectPlayer(player);
        }
        else if (Infected.Contains(player.Data) && !Infected.Contains(target.Data))
        {
            InfectPlayer(target);
        }
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        CheckPestillenceCondition();
    }

    private void InfectPlayer(PlayerControl player)
    {
        Infected.Add(player.Data);
        if (_player.IsLocalPlayer())
        {
            player.SetTrueVisorColor(Utils.HexToColor32(RoleColor));
            player.BetterData().NameColor = RoleColor;
        }

        CheckPestillenceCondition();
    }

    private void CheckPestillenceCondition()
    {
        if (Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(Infected.Contains))
        {
            if (playAnimation.GetBool())
            {
                PlayAnimation();
            }
            _player.SetRoleSync(CustomRoles.Pestillence);
        }
    }

    private void PlayAnimation()
    {
        _player.shapeshifting = true;
        _player.MyPhysics.SetNormalizedVelocity(Vector2.zero);
        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.shapeshiftAnim, _player.gameObject.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(_player.AmOwner);
        roleEffectAnimation.Renderer.material.SetColor("_BackColor", Utils.HexToColor32(RoleColor));
        roleEffectAnimation.Renderer.material.SetColor("_BodyColor", Utils.HexToColor32(RoleColor));
        roleEffectAnimation.Renderer.material.SetColor("_VisorColor", Palette.VisorColor);
        if (_player.cosmetics.FlipX)
        {
            roleEffectAnimation.transform.position -= new Vector3(0.14f, 0f, 0f);
        }
        roleEffectAnimation.MidAnimCB = (Action)(() =>
        {
            _player.cosmetics.SetScale(_player.MyPhysics.Animations.DefaultPlayerScale, _player.defaultCosmeticsScale);
            // (_player.Data.Role as ShapeshifterRole).SetEvidence();
        });
        float shapeshiftScale = _player.MyPhysics.Animations.ShapeshiftScale;
        if (AprilFoolsMode.ShouldLongAround())
        {
            _player.cosmetics.ShowLongModeParts(false);
            _player.cosmetics.SetHatVisorVisible(false);
        }
        _player.StartCoroutine(_player.ScalePlayer(shapeshiftScale, 0.25f));
        roleEffectAnimation.Play(_player, (Action)(() =>
        {
            _player.shapeshifting = false;
            if (AprilFoolsMode.ShouldLongAround())
            {
                _player.cosmetics.ShowLongModeParts(true);
                _player.cosmetics.SetHatVisorVisible(true);
            }
        }), PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
    }
}

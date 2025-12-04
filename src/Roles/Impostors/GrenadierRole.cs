using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class GrenadierRole : ImpostorRoleTBR, IRoleAbilityAction, IRoleMeetingAction
{
    internal sealed override int RoleId => 36;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Grenadier;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? FlashGrenadeCooldown;
    internal OptionItem? FlashGrenadeDuration;
    internal OptionItem? FlashGrenadeRadius;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                FlashGrenadeCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Grenadier.Option.FlashGrenadeCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
                FlashGrenadeDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Grenadier.Option.FlashGrenadeDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
                FlashGrenadeRadius = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Grenadier.Option.FlashGrenadeRadius", (1f, 5f, 0.25f), 3.5f, ("", "x"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private List<byte> flashed = [];
    internal BaseAbilityButton? FlashGrenadeButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            FlashGrenadeButton = RoleButtons.AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Grenadier.Ability.1"), FlashGrenadeCooldown.GetFloat(), 0, 0, null, this, true));
        }
    }

    internal sealed override void OnDeinitialize()
    {
        CustomSoundsManager.Instance.Stop("FlashBang");
        Clear();
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                {
                    if (_player.IsLocalPlayer())
                    {
                        BlindPlayers();
                    }
                }
                break;
        }
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        CustomSoundsManager.Instance.Stop("FlashBang");
        Clear();
    }

    private void BlindPlayers()
    {
        if (_player != null && _player.IsLocalPlayer())
        {
            CustomSoundsManager.Instance.Play(Sounds.FlashBang, 0.25f);
            Utils.FlashScreen("grenadier", new Color(0.8f, 0.8f, 0.8f, 0.35f), 0.15f, 2f, FlashGrenadeDuration.GetFloat(), true, true);
        }

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == null || target == _player || !target.IsAlive(true)) continue;

            if (Vector2.Distance(_player.GetTruePosition(), target.GetTruePosition()) < FlashGrenadeRadius.GetFloat()
                && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), target.GetTruePosition(), Constants.ShipOnlyMask, false)
                || Vector2.Distance(_player.GetTruePosition(), target.GetTruePosition()) < FlashGrenadeRadius.GetFloat() * 0.72f)
            {
                target.SetTrueVisorColor(Color.gray);
                flashed.Add(target.PlayerId);
                Networked.SendRoleSync(target);
            }
        }

        MarkDirty();
        _ = new LateTask(Clear, FlashGrenadeDuration.GetFloat(), shouldLog: false);
    }


    private void FlashPlayer(PlayerControl target)
    {
        if (target.IsLocalPlayer())
        {
            CustomSoundsManager.Instance.Play(Sounds.FlashBang);
            Utils.FlashScreen("grenadier", new Color(0.8f, 0.8f, 0.8f), 0.15f, 2f, FlashGrenadeDuration.GetFloat(), true, true);
            _ = new LateTask(Clear, FlashGrenadeDuration.GetFloat(), shouldLog: false);
        }
    }

    private void Clear()
    {
        if (_player.IsLocalPlayer())
        {
            ScreenFlash.Stop("grenadier");
        }

        if (flashed.Any())
        {
            foreach (var playerId in flashed)
            {
                var player = Utils.PlayerFromPlayerId(playerId);
                if (player == null) continue;

                if (_player.IsLocalPlayer())
                {
                    player.SetTrueVisorColor(Palette.VisorColor);
                }
                else if (player.IsLocalPlayer())
                {
                    ScreenFlash.Stop("grenadier");
                }
            }

            flashed.Clear();
        }
    }

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        FlashPlayer(data.MessageReader.ReadFast<PlayerControl>());
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WriteFast(flashed);
        ClearDirtyBits();
    }

    public override void Deserialize(MessageReader reader)
    {
        var bytes = reader.ReadFast<List<byte>>();
        flashed = bytes;
    }
}
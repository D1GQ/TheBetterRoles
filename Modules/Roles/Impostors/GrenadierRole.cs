using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class GrenadierRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 36;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Grenadier;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool DefaultVentOption => false;

    public BetterOptionItem? FlashGrenadeCooldown;
    public BetterOptionItem? FlashGrenadeDuration;
    public BetterOptionItem? FlashGrenadeRadius;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                FlashGrenadeCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Grenadier.Option.FlashGrenadeCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                FlashGrenadeDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Grenadier.Option.FlashGrenadeDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                FlashGrenadeRadius = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Grenadier.Option.FlashGrenadeRadius"), [1f, 5f, 0.25f], 3.5f, "x", "", RoleOptionItem),
            ];
        }
    }

    public BaseAbilityButton? FlashGrenadeButton = new();
    public override void OnSetUpRole()
    {
        FlashGrenadeButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Grenadier.Ability.1"), FlashGrenadeCooldown.GetFloat(), 0, 0, null, this, true));
    }

    public override void OnDeinitialize()
    {
        CustomSoundsManager.Stop("FlashBang");
        Clear();
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
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

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        CustomSoundsManager.Stop("FlashBang");
        Clear();
    }

    private List<byte> flashed = [];
    private void BlindPlayers()
    {
        if (_player != null && _player.IsLocalPlayer())
        {
            CustomSoundsManager.Play("FlashBang", 0.25f);
            Utils.FlashScreen(new Color(0.8f, 0.8f, 0.8f, 0.35f), 0.15f, 2f, FlashGrenadeDuration.GetFloat(), true, true);
        }

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == null || target == _player || !target.IsAlive(true)) continue;

            if (Vector2.Distance(_player.GetTruePosition(), target.GetTruePosition()) < FlashGrenadeRadius.GetFloat()
                && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), target.GetTruePosition(), Constants.ShipOnlyMask, false)
                || Vector2.Distance(_player.GetTruePosition(), target.GetTruePosition()) < FlashGrenadeRadius.GetFloat() * 0.72f)
            {
                target.SetTrueVisorColor(UnityEngine.Color.white);
                flashed.Add(target.PlayerId);
                SendRoleSync(0, [target]);
            }
        }

        IsDirty = true;
        _ = new LateTask(Clear, FlashGrenadeDuration.GetFloat(), shouldLog: false);
    }


    private void FlashPlayer(PlayerControl target)
    {
        if (target.IsLocalPlayer())
        {
            CustomSoundsManager.Play("FlashBang");
            Utils.FlashScreen(new Color(0.8f, 0.8f, 0.8f), 0.15f, 2f, FlashGrenadeDuration.GetFloat(), true, true);
            _ = new LateTask(Clear, FlashGrenadeDuration.GetFloat(), shouldLog: false);
        }
    }

    private void Clear()
    {
        if (_player.IsLocalPlayer())
        {
            Utils.FlashScreen(new Color(0.8f, 0.8f, 0.8f, 0.35f), 0f, 0f, 0f, true, true);
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
                    Utils.FlashScreen(new UnityEngine.Color(0.8f, 0.8f, 0.8f), 0f, 0f, 0f, true, true);
                }
            }

            flashed.Clear();
        }
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    if (additionalParams[0].TryCast<PlayerControl>(out var player))
                    {
                        writer.WritePlayerId(player);
                    }
                }
                break;
        }
    }

    public override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    var player = reader.ReadPlayerId();
                    if (player != null)
                    {
                        FlashPlayer(player);
                    }
                }
                break;
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePacked(flashed.Count);
        foreach (var vote in flashed)
        {
            writer.Write(vote);
        }
    }

    public override void Deserialize(MessageReader reader)
    {
        int count = reader.ReadPackedInt32();
        List<byte> bytes = [];
        for (int i = 0; i < count; i++)
        {
            bytes.Add(reader.ReadByte());
        }
        flashed = bytes;
    }
}
using Hazel;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class MorphlingRole : ImpostorRoleTBR, IRoleAbilityAction<PlayerControl>, IRoleMurderAction, IRoleSabotageAction
{
    internal sealed override int RoleId => 5;
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Morphling;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? SampleCooldown;
    internal OptionItem? TransformCooldown;
    internal OptionItem? TransformDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SampleCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Morphling.Option.SampleCooldown", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
                TransformCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Morphling.Option.TransformCooldown", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
                TransformDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Morphling.Option.TransformDuration", (0f, 180f, 2.5f), 15f, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private NetworkedPlayerInfo.PlayerOutfit? originalData;
    private NetworkedPlayerInfo.PlayerOutfit? sampleData;
    internal PlayerAbilityButton? SampleButton;
    internal BaseAbilityButton? TransformButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            SampleButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Morphling.Ability.1"), SampleCooldown.GetFloat(), 0, 0, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
            SampleButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.sampleData == null; };

            TransformButton = RoleButtons.AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Morphling.Ability.2"), TransformCooldown.GetFloat(), TransformDuration.GetFloat(), 0, null, this, true));
            TransformButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.sampleData != null; };
            TransformButton.InteractCondition = () => { return !GameState.IsSystemActive(SystemTypes.MushroomMixupSabotage) && _player.ExtendedPC().CamouflagedQueue; };
            TransformButton.DurationName = Translator.GetString("Role.Morphling.Ability.3");
            TransformButton.CanCancelDuration = true;
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                DisguisedTargetId = target.Data.PlayerId;
                sampleData = CopyOutfit(target.Data);
                TransformButton?.SetCooldown();
                SendRoleSync(0, target);
                break;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 6:
                if (sampleData != null)
                {
                    IsDisguised = true;
                    RoleListener.InvokeRoles<IRoleDisguiseAction>(role => role.Disguise(_player), player: _player);
                    originalData = CopyOutfit(_data);
                    SetOutfit(sampleData);
                    TransformButton?.SetDuration();
                    _player.RawSetName(Utils.FormatPlayerName(_player));
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 6:
                if (originalData != null)
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    private static NetworkedPlayerInfo.PlayerOutfit CopyOutfit(NetworkedPlayerInfo data)
    {
        var outfit = data.DefaultOutfit;
        return new NetworkedPlayerInfo.PlayerOutfit()
        {
            PlayerName = outfit.PlayerName,
            ColorId = outfit.ColorId,
            HatId = outfit.HatId,
            PetId = outfit.PetId,
            SkinId = outfit.SkinId,
            VisorId = outfit.VisorId,
            NamePlateId = outfit.NamePlateId
        };
    }

    private void SetOutfit(NetworkedPlayerInfo.PlayerOutfit outfit)
    {
        _player.RawSetOutfit(outfit, PlayerOutfitType.Default);
    }

    void IRoleMurderAction.MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            ((IRoleAbilityAction)this).OnResetAbilityState(false);
        }
    }

    void IRoleSabotageAction.OnSabotage(ISystemType system, SystemTypes? systemType)
    {
        if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            ((IRoleAbilityAction)this).OnResetAbilityState(false);
            return;
        }

        if (systemType == SystemTypes.Comms && TBRGameSettings.CamouflageComms.GetBool())
        {
            if (system.TryCast<HqHudSystemType>(out var hqHudSystem))
            {
                if (!hqHudSystem.IsActive)
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(false);
                }
            }
            else if (system.TryCast<HudOverrideSystemType>(out var hudOverrideSystem))
            {
                if (!hudOverrideSystem.IsActive)
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(false);
                }
            }
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool IsTimeOut)
    {
        if (!IsDisguised) return;

        IsDisguised = false;
        DisguisedTargetId = -1;
        if (sampleData != null)
        {
            if (originalData != null)
            {
                RoleListener.InvokeRoles<IRoleDisguiseAction>(role => role.Undisguise(_player), player: _player);
                SetOutfit(originalData);
            }
            sampleData = null;
            originalData = null;
            TransformButton?.SetCooldown(durationState: 0);
        }
        SampleButton?.SetCooldown();
    }

    internal override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    var target = reader.ReadFast<PlayerControl>();
                    DisguisedTargetId = target.Data.PlayerId;
                    sampleData = CopyOutfit(target.Data);
                }
                break;
            case 1:
                if (sampleData != null)
                {
                    IsDisguised = true;
                    RoleListener.InvokeRoles<IRoleDisguiseAction>(role => role.Disguise(_player), player: _player);
                    originalData = CopyOutfit(_data);
                    SetOutfit(sampleData);
                    TransformButton?.SetDuration();
                    _player.RawSetName(Utils.FormatPlayerName(_player));
                }
                break;
        }
    }
}

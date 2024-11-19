
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class MorphlingRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 5;
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Morphling;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override TBROptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool DefaultVentOption => false;

    public TBROptionItem? SampleCooldown;
    public TBROptionItem? TransformCooldown;
    public TBROptionItem? TransformDuration;

    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SampleCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Morphling.Option.SampleCooldown"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                TransformCooldown = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Morphling.Option.TransformCooldown"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                TransformDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Morphling.Option.TransformDuration"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private NetworkedPlayerInfo.PlayerOutfit? originalData;
    private NetworkedPlayerInfo.PlayerOutfit? sampleData;
    public PlayerAbilityButton? SampleButton;
    public BaseAbilityButton? TransformButton;

    public override void OnSetUpRole()
    {
        SampleButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Morphling.Ability.1"), SampleCooldown.GetFloat(), 0, 0, null, this, true, VanillaGameSettings.KillDistance.GetValue()));
        SampleButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.sampleData == null; };

        TransformButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Morphling.Ability.2"), TransformCooldown.GetFloat(), TransformDuration.GetFloat(), 0, null, this, true));
        TransformButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.sampleData != null; };
        TransformButton.InteractCondition = () => { return !GameState.IsSystemActive(SystemTypes.MushroomMixupSabotage) && _player.ExtendedData().CamouflagedQueue; };
        TransformButton.DurationName = Translator.GetString("Role.Morphling.Ability.3");
        TransformButton.CanCancelDuration = true;
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                if (target?.Data != null)
                {
                    DisguisedTargetId = target.Data.PlayerId;
                    sampleData = CopyOutfit(target.Data);
                    TransformButton?.SetCooldown();
                }
                break;
            case 6:
                if (sampleData != null)
                {
                    IsDisguised = true;
                    CustomRoleManager.RoleListener(_player, role => role.OnDisguise(_player));
                    originalData = CopyOutfit(_data);
                    SetOutfit(sampleData);
                    TransformButton?.SetDuration();
                    _player.RawSetName(Utils.FormatPlayerName(_player.Data));
                }
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 6:
                if (originalData != null)
                {
                    OnResetAbilityState(isTimeOut);
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

    public override void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            OnResetAbilityState(false);
        }
    }

    public override void OnSabotage(ISystemType system, SystemTypes? systemType)
    {
        if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            OnResetAbilityState(false);
            return;
        }

        if (systemType == SystemTypes.Comms && TBRGameSettings.CamouflageComms.GetBool())
        {
            if (system.TryCast<HqHudSystemType>(out var hqHudSystem))
            {
                if (!hqHudSystem.IsActive)
                {
                    OnResetAbilityState(false);
                }
            }
            else if (system.TryCast<HudOverrideSystemType>(out var hudOverrideSystem))
            {
                if (!hudOverrideSystem.IsActive)
                {
                    OnResetAbilityState(false);
                }
            }
        }
    }

    public override void OnResetAbilityState(bool IsTimeOut)
    {
        if (!IsDisguised) return;

        IsDisguised = false;
        DisguisedTargetId = -1;
        if (sampleData != null)
        {
            if (originalData != null)
            {
                CustomRoleManager.RoleListener(_player, role => role.OnUndisguise(_player));
                SetOutfit(originalData);
            }
            sampleData = null;
            originalData = null;
            TransformButton?.SetCooldown(durationState: 0);
        }
        SampleButton?.SetCooldown();
    }
}

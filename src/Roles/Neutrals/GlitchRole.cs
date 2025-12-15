using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class GlitchRole : RoleClass, IRoleAbilityAction<PlayerControl>, IRoleMurderAction, IRoleSabotageAction, IRoleUpdateAction, IRoleMenuAction
{
    internal sealed override int RoleId => 19;
    internal sealed override bool VentReliantRole => true;
    internal sealed override bool CanVent => false;
    internal sealed override string RoleColorHex => "#32ff00";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Glitch;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override bool CanKill => true;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? HackCooldown;
    internal OptionItem? HackDuration;
    internal OptionItem? HackDistance;
    internal OptionItem? MimicCooldown;
    internal OptionItem? MimicDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                HackCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Glitch.Option.HackCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                HackDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Glitch.Option.HackDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
                HackDistance = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Glitch.Option.HackDistance", ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 1, RoleOptions.RoleOptionItem),

                MimicCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Glitch.Option.MimicCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                MimicDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Glitch.Option.MimicDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private NetworkedPlayerInfo? hacked;
    private float tempHackDuration = 0f;
    private NetworkedPlayerInfo.PlayerOutfit? originalData;
    private PlayerMenu? Menu;
    internal PlayerAbilityButton? HackButton = new();
    internal BaseAbilityButton? MimicButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            HackButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Glitch.Ability.1"), HackCooldown.GetFloat(), 0, 0, null, this, true, HackDistance.GetStringValue()));
            MimicButton = RoleButtons.AddButton(BaseAbilityButton.Create(6, Translator.GetString("Role.Glitch.Ability.2"), MimicCooldown.GetFloat(), MimicDuration.GetFloat(), 0, null, this, false));
            MimicButton.InteractCondition = () => { return !GameState.IsSystemActive(SystemTypes.MushroomMixupSabotage) && _player.ExtendedPC().CamouflagedQueue; };
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    AddHack(target);
                    Networked.SendRoleSync(1, target);
                }
                break;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 6:
                {
                    MimicButton?.SetCooldown(0);
                    if (_player.IsLocalPlayer())
                        Menu = new PlayerMenu().Create(id, this, true, true, false);
                }
                break;
        }
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (hacked != null)
        {
            if (tempHackDuration > 0f)
            {
                tempHackDuration -= Time.deltaTime;
            }
            else
            {
                RemoveHack();
            }
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool IsTimeOut)
    {
        ResetMimic();
        RemoveHack();
    }

    private Shader? catchShader;
    private void AddHack(PlayerControl target)
    {
        if (_player.IsLocalPlayer()) target.ShieldBreakAnimation(RoleColorHex);
        hacked = target.Data;
        tempHackDuration = HackDuration.GetFloat();
        if (target.IsLocalPlayer())
        {
            catchShader = HudManager.Instance.UseButton.graphic.material.shader;

            List<BaseButton> buttons = BaseButton.allButtons;
            foreach (var button in buttons)
            {
                button.Hacked = true;
                button.ActionButton.graphic.material.shader = AssetBundles.GlitchShader;
            }

            HudManager.Instance.UseButton.enabled = false;
            HudManager.Instance.UseButton.graphic.material.shader = AssetBundles.GlitchShader;
            HudManager.Instance.PetButton.enabled = false;
            HudManager.Instance.PetButton.graphic.material.shader = AssetBundles.GlitchShader;
        }
    }

    private void RemoveHack()
    {
        if (hacked != null && hacked.IsLocalData())
        {
            List<BaseButton> buttons = BaseButton.allButtons;
            foreach (var button in buttons)
            {
                button.Hacked = false;
                button.ActionButton.graphic.material.shader = catchShader;
            }

            HudManager.Instance.UseButton.enabled = true;
            HudManager.Instance.UseButton.graphic.material.shader = catchShader;
            HudManager.Instance.PetButton.enabled = true;
            HudManager.Instance.PetButton.graphic.material.shader = catchShader;
        }
        tempHackDuration = 0f;
        hacked = null;
    }

    void IRoleMenuAction.PlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close)
    {
        switch (id)
        {
            case 6:
                {
                    if (target != null)
                    {
                        menu?.PlayerMinigame.Close();
                        SetMimic(targetData);
                        Networked.SendRoleSync(0, targetData);
                        MimicButton?.SetDuration();
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 6:
                {
                    ResetMimic();
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
            ResetMimic();
        }
    }

    void IRoleSabotageAction.OnSabotage(ISystemType system, SystemTypes? systemType)
    {
        void Reset()
        {
            if (IsDisguised)
            {
                ResetMimic();
            }
        }

        if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            Reset();
            return;
        }

        if (systemType == SystemTypes.Comms && TBRGameSettings.CamouflageComms.GetBool())
        {
            if (system.TryCast<HqHudSystemType>(out var hqHudSystem))
            {
                if (!hqHudSystem.IsActive)
                {
                    Reset();
                }
            }
            else if (system.TryCast<HudOverrideSystemType>(out var hudOverrideSystem))
            {
                if (!hudOverrideSystem.IsActive)
                {
                    Reset();
                }
            }
        }
    }

    private void SetMimic(NetworkedPlayerInfo target)
    {
        DisguisedTargetId = target.PlayerId;
        IsDisguised = true;
        originalData = CopyOutfit(_data);
        SetOutfit(target.DefaultOutfit);
        _player.RawSetName(Utils.FormatPlayerName(_player));
    }

    private void ResetMimic()
    {
        if (!IsDisguised) return;

        IsDisguised = false;
        DisguisedTargetId = -1;

        if (originalData != null)
        {
            SetOutfit(originalData);
            MimicButton.SetCooldown(durationState: 0);
        }
        originalData = null;
    }

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                {
                    var playerData = data.MessageReader.ReadFast<NetworkedPlayerInfo>();
                    if (playerData != null)
                    {
                        SetMimic(playerData);
                    }
                }
                break;
            case 1:
                {
                    AddHack(data.MessageReader.ReadFast<PlayerControl>());
                }
                break;
        }
    }
}

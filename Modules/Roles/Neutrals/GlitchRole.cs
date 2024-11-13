
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class GlitchRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 19;
    public override bool VentReliantRole => true;
    public override bool CanVent => false;
    public override string RoleColor => "#32ff00";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Glitch;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? HackCooldown;
    public BetterOptionItem? HackDuration;
    public BetterOptionItem? HackDistance;
    public BetterOptionItem? MimicCooldown;
    public BetterOptionItem? MimicDuration;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                HackCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Glitch.Option.HackCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                HackDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Glitch.Option.HackDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                HackDistance = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Glitch.Option.HackDistance"),
                [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 1, RoleOptionItem),

                MimicCooldown = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Glitch.Option.MimicCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                MimicDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Glitch.Option.MimicDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
            ];
        }
    }

    private NetworkedPlayerInfo? hacked;
    private float tempHackDuration = 0f;
    private NetworkedPlayerInfo.PlayerOutfit? originalData;
    private PlayerMenu? Menu;

    public PlayerAbilityButton? HackButton = new();
    public BaseAbilityButton? MimicButton = new();
    public override void OnSetUpRole()
    {
        HackButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Glitch.Ability.1"), HackCooldown.GetFloat(), 0, 0, null, this, true, HackDistance.GetValue()));
        MimicButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Glitch.Ability.2"), MimicCooldown.GetFloat(), MimicDuration.GetFloat(), 0, null, this, false));
        MimicButton.InteractCondition = () => { return !GameState.IsSystemActive(SystemTypes.MushroomMixupSabotage); };
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    AddHack(target);
                }
                break;
            case 6:
                {
                    MimicButton?.SetCooldown(0);
                    if (_player.IsLocalPlayer())
                        Menu = new PlayerMenu().Create(id, this, true, true, false);
                }
                break;
        }
    }

    public override void FixedUpdate()
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

    public override void OnResetAbilityState(bool IsTimeOut)
    {
        ResetMimic();
        RemoveHack();
    }

    private void AddHack(PlayerControl target)
    {
        if (_player.IsLocalPlayer()) target.ShieldBreakAnimation(RoleColor);
        hacked = target.Data;
        tempHackDuration = HackDuration.GetFloat();
        if (target.IsLocalPlayer())
        {
            List<BaseButton> buttons = BaseButton.allButtons.ToList();
            foreach (var button in buttons)
            {
                button.Hacked = true;
            }

            ReportButton.Hacked = true;
            HudManager.Instance.UseButton.GetComponent<ActionButton>().enabled = false;
        }
    }

    private void RemoveHack()
    {
        if (hacked != null && hacked.IsLocalData())
        {
            List<BaseButton> buttons = BaseButton.allButtons.ToList();
            foreach (var button in buttons)
            {
                button.Hacked = false;
            }

            ReportButton.Hacked = false;
            HudManager.Instance.UseButton.GetComponent<ActionButton>().enabled = true;
        }
        tempHackDuration = 0f;
        hacked = null;
    }

    public override void OnPlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close)
    {
        switch (id)
        {
            case 6:
                {
                    if (target != null)
                    {
                        menu?.PlayerMinigame.Close();
                        SetMimic(targetData);
                        SendRoleSync(0, [targetData]);
                        MimicButton?.SetDuration();
                    }
                }
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
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

    public override void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            ResetMimic();
        }
    }

    public override void OnSabotage(ISystemType system, SystemTypes? systemType)
    {
        if (systemType == SystemTypes.MushroomMixupSabotage)
        {
            if (IsDisguised)
            {
                ResetMimic();
            }
        }
    }

    private void SetMimic(NetworkedPlayerInfo target)
    {
        DisguisedTargetId = target.PlayerId;
        IsDisguised = true;
        originalData = CopyOutfit(_data);
        SetOutfit(target.DefaultOutfit);
        _player.RawSetName(Utils.FormatPlayerName(_player.Data));
    }

    private void ResetMimic()
    {
        if (!IsDisguised) return;

        IsDisguised = false;
        DisguisedTargetId = -1;

        if (originalData != null)
        {
            CustomRoleManager.RoleListener(_player, role => role.OnUndisguise(_player));
            SetOutfit(originalData);
            MimicButton.SetCooldown(durationState: 0);
        }
        originalData = null;
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    writer.WritePlayerDataId((NetworkedPlayerInfo)additionalParams[0]);
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
                    var data = reader.ReadPlayerDataId();
                    if (data != null)
                    {
                        SetMimic(data);
                    }
                }
                break;
        }
    }
}

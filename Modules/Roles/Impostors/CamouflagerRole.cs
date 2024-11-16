using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class CamouflagerRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 37;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Camouflager;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool DefaultVentOption => false;

    public BetterOptionItem? CamouflageCooldown;
    public BetterOptionItem? CamouflageDuration;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CamouflageCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Camouflager.Option.CamouflageCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                CamouflageDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Camouflager.Option.CamouflageDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
            ];
        }
    }

    public BaseAbilityButton? CamouflageButton = new();
    bool camouflageActive = false;
    public override void OnSetUpRole()
    {
        CamouflageButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Camouflager.Ability.1"), CamouflageCooldown.GetFloat(), CamouflageDuration.GetFloat(), 0, null, this, true));
        CamouflageButton.CanCancelDuration = true;
        CamouflageButton.InteractCondition = () => { return !GameState.CamouflageCommsIsActive || CamouflageButton.IsDuration; };
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (!camouflageActive)
                    {
                        Camouflage(true);
                        CamouflageButton?.SetDuration();
                    }
                }
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        OnResetAbilityState(false);
    }

    private void Camouflage(bool active)
    {
        camouflageActive = active;
        foreach (var player in Main.AllPlayerControls)
        {
            player.SetCamouflage(active);
        }
    }

    public override void OnResetAbilityState(bool isTimeOut)
    {
        if (camouflageActive)
        {
            Camouflage(false);
        }
    }
}
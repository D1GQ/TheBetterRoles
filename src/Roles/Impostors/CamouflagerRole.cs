using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class CamouflagerRole : ImpostorRoleTBR, IRoleAbilityAction
{
    internal sealed override int RoleId => 37;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Camouflager;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? CamouflageCooldown;
    internal OptionItem? CamouflageDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CamouflageCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Camouflager.Option.CamouflageCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
                CamouflageDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Camouflager.Option.CamouflageDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool camouflageActive = false;
    internal BaseAbilityButton? CamouflageButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            CamouflageButton = RoleButtons.AddButton(BaseAbilityButton.Create(5, Translator.GetString("Role.Camouflager.Ability.1"), CamouflageCooldown.GetFloat(), CamouflageDuration.GetFloat(), 0, null, this, true));
            CamouflageButton.CanCancelDuration = true;
            CamouflageButton.InteractCondition = () => { return !GameState.CamouflageCommsIsActive || CamouflageButton.IsDuration; };
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                {
                    if (!camouflageActive)
                    {
                        Camouflage(true);
                        Networked.SendRoleSync(true);
                        CamouflageButton?.SetDuration();
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    private void Camouflage(bool active)
    {
        camouflageActive = active;
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            player.SetCamouflage(active);
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        if (camouflageActive)
        {
            Camouflage(false);
            Networked.SendRoleSync(false);
        }
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        Camouflage(data.MessageReader.ReadFast<bool>());
    }
}
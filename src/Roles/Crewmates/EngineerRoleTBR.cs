using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class EngineerRoleTBR : CrewmateRoleTBR, IRoleAbilityAction
{
    internal sealed override int RoleId => 43;
    internal sealed override string RoleColorHex => "#E6A063";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Engineer;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal sealed override AudioClip? IntroSound => Prefab.GetCachedPrefab<EngineerRole>().IntroSound;
    internal sealed override bool CanVent => true;

    internal OptionItem? AmountOfFixes;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                AmountOfFixes = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Engineer.Option.AmountOfFixes", (1, 100, 1), 2, ("", ""), RoleOptions.RoleOptionItem)
            ];
        }
    }

    internal BaseAbilityButton? FixButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            FixButton = RoleButtons.AddButton(BaseAbilityButton.Create(5, Translator.GetString("Role.Engineer.Ability.1"), 5f, 0, AmountOfFixes.GetInt(), null, this, true));
            FixButton.InteractCondition = () => { return GameState.IsAnySabotageActive; };
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                {
                    Networked.SendRoleSync(0);
                }
                break;
        }
    }

    private static void FixActiveSabotages() => Utils.ClearAllSabotages();

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        if (GameState.IsHost && data.Sender == _player)
        {
            FixActiveSabotages();
        }
    }
}
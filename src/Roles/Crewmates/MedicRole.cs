using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;
using static TheBetterRoles.Modules.Translator;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class MedicRole : CrewmateRoleTBR, IRoleMurderAction, IRoleAbilityAction<PlayerControl>, IRoleDeathAction
{
    internal sealed override int RoleId => 11;
    internal sealed override string RoleColorHex => "#00FF2A";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Medic;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal sealed override AudioClip? IntroSound => Prefab.GetCachedPrefab<ScientistRole>().IntroSound;

    internal OptionItem? ShowShieldedPlayer;
    internal OptionItem? Notify;
    internal OptionItem? NotifyKiller;
    internal OptionItem? RemoveShieldOnMedicDeath;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShowShieldedPlayer = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Medic.Option.ShowShieldedPlayer",
                [$"{Utils.GetCustomRoleNameAndColor(RoleType)}", $"{Utils.GetCustomRoleNameAndColor(RoleType)}+<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>", $"{GetString("All")}"], 0, RoleOptions.RoleOptionItem),

                Notify = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Medic.Option.Notify",
                [$"{Utils.GetCustomRoleNameAndColor(RoleType)}", $"<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>", $"{Utils.GetCustomRoleNameAndColor(RoleType)}+<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>"], 0, RoleOptions.RoleOptionItem),

                NotifyKiller = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Medic.Option.NotifyKiller", true, RoleOptions.RoleOptionItem),
                RemoveShieldOnMedicDeath = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Medic.Option.RemoveShieldOnMedicDeath", false, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private NetworkedPlayerInfo? Shielded;
    internal PlayerAbilityButton? ShieldButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            ShieldButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, "Shield", 0, 0, 1, null, this, true, 1));
        }
    }

    internal sealed override void OnDeinitialize()
    {
        RemoveShield();
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    AddShield(target);
                    Networked.SendRoleSync(0, target);
                }
                break;
        }
    }

    private void AddShield(PlayerControl player)
    {
        Shielded = player.Data;
        player.UpdateName();
    }

    private void RemoveShield()
    {
        var player = Shielded.Object;
        Shielded = null;
        player?.UpdateName();
    }

    void IRoleDeathAction.OnDeath(PlayerControl player, DeathReasons reason)
    {
        if (Shielded != null && RemoveShieldOnMedicDeath.GetBool())
        {
            RemoveShield();
        }
    }

    bool IRoleMurderAction.CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target.Data == Shielded)
        {
            if (_player.IsLocalPlayer() && Notify.GetStringValue() is 0 or 2
                || target.IsLocalPlayer() && Notify.GetStringValue() is 1 or 2)
            {
                Utils.FlashScreen("medic", RoleColorHex);
            }

            if (killer.IsLocalPlayer() && NotifyKiller.GetBool())
            {
                target.ShieldBreakAnimation(RoleColorHex);
            }
            killer?.Role()?.RoleButtons.KillButton?.SetCooldown();

            return false;
        }

        return true;
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (ShowShieldedPlayer.GetStringValue() == 0 && !_player.IsLocalPlayer() && localPlayer.IsAlive()) return string.Empty;
        if (ShowShieldedPlayer.GetStringValue() == 1 && !_player.IsLocalPlayer() && localPlayer.Data != Shielded && localPlayer.IsAlive()) return string.Empty;

        if (target.Data == Shielded)
        {
            return $"<{RoleColorHex}>✚</color>";
        }

        return string.Empty;
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        AddShield(data.MessageReader.ReadFast<PlayerControl>());
    }
}


using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using static TheBetterRoles.Modules.Translator;

namespace TheBetterRoles.Roles;

public class MedicRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 11;
    public override string RoleColor => "#00FF2A";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Medic;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? ShowShieldedPlayer;
    public BetterOptionItem? Notify;
    public BetterOptionItem? NotifyKiller;
    public BetterOptionItem? RemoveShieldOnMedicDeath;

    public PlayerAbilityButton? ShieldButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ShowShieldedPlayer = new BetterOptionStringItem().Create(GetOptionUID(true), SettingsTab, GetString("Role.Medic.Option.ShowShieldedPlayer"),
                [$"{Utils.GetCustomRoleNameAndColor(RoleType)}", $"{Utils.GetCustomRoleNameAndColor(RoleType)}+<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>", $"{GetString("All")}"], 0, RoleOptionItem),

                Notify = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, GetString("Role.Medic.Option.Notify"),
                [$"{Utils.GetCustomRoleNameAndColor(RoleType)}", $"<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>", $"{Utils.GetCustomRoleNameAndColor(RoleType)}+<#8E8E8E>{GetString("Role.Medic.Shielded")}</color>"], 0, RoleOptionItem),

                NotifyKiller = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab, GetString("Role.Medic.Option.NotifyKiller"), true, RoleOptionItem),
                RemoveShieldOnMedicDeath = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab, GetString("Role.Medic.Option.RemoveShieldOnMedicDeath"), false, RoleOptionItem),
            ];
        }
    }

    public override void OnSetUpRole()
    {
        ShieldButton = AddButton(new PlayerAbilityButton().Create(5, "Shield", 0, 1, null, this, true, 1));
    }

    public override void OnDeinitialize()
    {
        if (Shielded != null)
        {
            Shielded = null;
        }
    }

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == _player)
        {
            if (Shielded != null && RemoveShieldOnMedicDeath.GetBool())
            {
                Shielded = null;
            }
        }
    }

    private NetworkedPlayerInfo? Shielded;
    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null)
                    {
                        Shielded = target.Data;
                    }
                }
                break;
        }
    }

    public override bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target.Data == Shielded)
        {
            if (_player.IsLocalPlayer() && Notify.GetValue() is 0 or 2
                || target.IsLocalPlayer() && Notify.GetValue() is 1 or 2)
            {
                Utils.FlashScreen(RoleColor);
            }

            if (killer.IsLocalPlayer() && NotifyKiller.GetBool())
            {
                target.ShieldBreakAnimation(RoleColor);
            }
            killer.Role().KillButton.SetCooldown();

            return false;
        }

        return true;
    }

    public override string SetNameMark(PlayerControl target)
    {
        if (ShowShieldedPlayer.GetValue() == 0 && !_player.IsLocalPlayer()) return string.Empty;
        if (ShowShieldedPlayer.GetValue() == 1 && !_player.IsLocalPlayer() && localPlayer.Data != Shielded) return string.Empty;

        if (target.Data == Shielded)
        {
            return $"<{RoleColor}>✚</color>";
        }

        return string.Empty;
    }
}

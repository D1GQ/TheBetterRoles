using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class VultureRole : RoleClass, IRoleAbilityAction<DeadBody>, IRoleMurderAction, IRoleMeetingAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 48;
    internal sealed override string RoleColorHex => "#5F6F50";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Vulture;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Evil;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? EatCooldown;
    internal OptionItem? AmountToEat;
    internal OptionItem? MaximaUsesPerMeeting;
    internal OptionItem? ShowArrowsToBodies;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                EatCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Vulture.Option.EatCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                AmountToEat = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Vulture.Option.AmountToEat", (1, 15, 1), 3, ("", ""), RoleOptions.RoleOptionItem),
                MaximaUsesPerMeeting = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Vulture.Option.MaximaUsesPerMeeting", (1, 100, 1), 1, ("", ""), RoleOptions.RoleOptionItem, canBeInfinite: true),
                ShowArrowsToBodies = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Vulture.Option.ShowArrowsToBodies", true, RoleOptions.RoleOptionItem),
            ];
        }
    }

    internal int EatenBodies = 0;
    internal int BodiesToEat = int.MaxValue;
    internal int MaximaUses;
    internal DeadBodyAbilityButton? EatButton = new();
    internal sealed override void OnSetUpRole()
    {
        MaximaUses = MaximaUsesPerMeeting.GetInt();
        BodiesToEat = AmountToEat.GetInt();
        if (_player.IsLocalPlayer())
        {
            EatButton = RoleButtons.AddButton(DeadBodyAbilityButton.Create(5, Translator.GetString("Role.Vulture.Ability.1"), EatCooldown.GetFloat(), 0, MaximaUses, null, this, true, 0));
        }
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        if (MaximaUses > 0)
        {
            EatButton.SetUses(MaximaUses);
        }
    }

    void IRoleAbilityAction<DeadBody>.OnAbility(int id, DeadBody target)
    {
        switch (id)
        {
            case 5:
                EatBody(target);
                Networked.SendRoleSync(target);
                break;
        }
    }

    void IRoleMurderAction.DeadBodyDropOther(PlayerControl killer, DeadBody body)
    {
        if (ShowArrowsToBodies.GetBool() && _player.IsLocalPlayer() && _player.IsAlive())
        {
            var arrow = ArrowLocator.Create(color: RoleColor, maxScale: 0.7f, minDistance: 0f);
            arrow.SetTarget(body.gameObject);
            arrow.RemoveListener = () => { return body == null || body.TruePosition.y > 500 || _player?.IsAlive() != true; };
        }
    }

    private void EatBody(DeadBody body)
    {
        EatenBodies++;
        body.DestroyObj();
        if (EatenBodies >= BodiesToEat)
        {
            CheckWinCondition();
        }
    }

    internal sealed override void SetAbilityAmountText(ref int maxAmount, ref int currentAmount)
    {
        maxAmount = BodiesToEat;
        currentAmount = EatenBodies;
    }

    internal sealed override void FormatRoleInfo(ref string info, bool isLongInfo)
    {
        if (!isLongInfo)
        {
            info = string.Format(info, BodiesToEat);
        }
    }

    bool IRoleGameplayAction.WinCondition() => EatenBodies >= BodiesToEat;

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        EatBody(data.MessageReader.ReadFast<DeadBody>());
    }
}
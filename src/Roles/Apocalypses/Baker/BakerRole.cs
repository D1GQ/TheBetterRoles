using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Apocalypses;

internal class BakerRole : RoleClass, IRoleAbilityAction<PlayerControl>, IRoleMeetingAction
{
    internal sealed override int RoleId => 51;
    internal sealed override string RoleColorHex => "#8C7451";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Baker;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Apocalypse;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Experimental;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ApocalypseRoles;
    internal sealed override AudioClip? IntroSound => Prefab.GetCachedPrefab<ShapeshifterRole>().IntroSound;
    internal sealed override bool IsBenign => true;

    internal OptionItem? BreadCooldown;
    internal OptionItem? FamineStarveCooldown;
    internal OptionItem? AbilityDistance;
    internal OptionItem? NumberOfBread;
    internal OptionItem? BreadEffects; // Add later
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                BreadCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Baker.Option.BreadCooldown", (0f, 180f, 2.5f), 25, ("", "s"), RoleOptions.RoleOptionItem),
                FamineStarveCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Baker.Option.FamineStarveCooldown", (0f, 180f, 2.5f), 25, ("", "s"), RoleOptions.RoleOptionItem),
                AbilityDistance = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Baker.Option.AbilityDistance", ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 1, RoleOptions.RoleOptionItem),
                NumberOfBread = OptionIntItem.Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Baker.Option.NumberOfBread"), (1, 14, 1), 3, ("", ""), RoleOptions.RoleOptionItem),
                // BreadEffects = TBROptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Baker.Option.BreadEffects", true, RoleOptionItem)
            ];
        }
    }

    private readonly List<NetworkedPlayerInfo> fedPlayers = [];
    internal PlayerAbilityButton? BreadButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            BreadButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Baker.Ability.1"), BreadCooldown.GetFloat(), 0, 1, null, this, true, AbilityDistance.GetStringValue()));
            BreadButton.TargetCondition = (target) =>
            {
                return !fedPlayers.Contains(target.Data);
            };
            BreadButton.AddVisibleCondition(() => { return fedPlayers.Where(data => data.IsAlive()).Count() < NumberOfBread.GetInt(); });
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    FeedPlayer(target);
                    Networked.SendRoleSync(target);
                }
                break;
        }
    }

    private void FeedPlayer(PlayerControl target)
    {
        fedPlayers.Add(target.Data);
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        if (_player.IsLocalPlayer())
        {
            BreadButton.SetUses(1);
        }

        if (fedPlayers.Where(data => data.IsAlive() && !data.IsLocalData()).Count() >= NumberOfBread.GetInt())
        {
            if (_player.IsLocalPlayer()) CustomSoundsManager.Instance.Play(Sounds.Transform);
            var role = CustomRoleManager.SetCustomRole(_player, RoleClassTypes.Famine);
            if (role is FamineRole Famine)
            {
                Famine.WasTransformed = true;
                Famine.StarvePlayers = Main.AllPlayerControls.Select(pc => pc.Data).Where(data => data.IsAlive() && !data.IsLocalData() && !fedPlayers.Contains(data)).ToList();
            }
        }
    }

    internal sealed override void SetAbilityAmountText(ref int maxAmount, ref int currentAmount)
    {
        maxAmount = NumberOfBread.GetInt();
        currentAmount = fedPlayers.Where(data => data.IsAlive()).Count();

    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (_player.IsLocalPlayer() && target.IsAlive() && fedPlayers.Contains(target.Data))
        {
            return "☗".ToColor(RoleColorHex);
        }

        return string.Empty;
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        FeedPlayer(data.MessageReader.ReadFast<PlayerControl>());
    }
}

using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class GlitchRole : CustomRoleBehavior
{
    // Role Info
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

    public TargetButton? HackButton = new();
    public AbilityButton? MimicButton = new();
    public override void OnSetUpRole()
    {
        HackButton = AddButton(new TargetButton().Create(5, Translator.GetString("Role.Glitch.Ability.1"), HackCooldown.GetFloat(), 0, null, this, true, HackDistance.GetValue()));
        MimicButton = AddButton(new AbilityButton().Create(6, Translator.GetString("Role.Glitch.Ability.2"), MimicCooldown.GetFloat(), MimicDuration.GetFloat(), 0, null, this, true));
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
                    MimicButton.SetCooldown(0);
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
        hacked = target.Data;
        tempHackDuration = HackDuration.GetFloat();
        if (target.IsLocalPlayer())
        {
            List<BaseButton> buttons = BaseButton.allButtons.ToList();
            foreach (var button in buttons)
            {
                button.Hacked = true;
            }

            HudManager.Instance.UseButton.GetComponent<ActionButton>().enabled = false;
            HudManager.Instance.ReportButton.GetComponent<ActionButton>().enabled = false;
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

            HudManager.Instance.UseButton.GetComponent<ActionButton>().enabled = true;
            HudManager.Instance.ReportButton.GetComponent<ActionButton>().enabled = true;
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
                        DisguisedTargetId = targetData.PlayerId;
                        IsDisguised = true;
                        originalData = CopyOutfit(_data);
                        SetOutfit(targetData.DefaultOutfit);
                        MimicButton.SetDuration();
                        _player.RawSetName(Utils.FormatPlayerName(_player.Data));
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
                    MimicButton.SetCooldown();
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
        _player.SetOutfit(outfit, PlayerOutfitType.Default);
    }

    public override void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            ResetMimic();
        }
    }

    private void ResetMimic()
    {
        IsDisguised = false;
        DisguisedTargetId = -1;

        if (originalData != null)
        {
            CustomRoleManager.RoleListener(_player, role => role.OnUndisguise(_player));
            SetOutfit(originalData);
        }
        originalData = null;
    }
}

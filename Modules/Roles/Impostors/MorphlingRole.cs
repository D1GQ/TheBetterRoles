
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class MorphlingRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 400;
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Morphling;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => AllowVenting.GetBool();
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? SampleCooldown;
    public BetterOptionItem? TransformCooldown;
    public BetterOptionItem? TransformDuration;
    public BetterOptionItem? AllowVenting;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                SampleCooldown = new BetterOptionFloatItem().Create(RoleId + 10, SettingsTab, Translator.GetString("Role.Morphling.Option.SampleCooldown"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                TransformCooldown = new BetterOptionFloatItem().Create(RoleId + 15, SettingsTab, Translator.GetString("Role.Morphling.Option.TransformCooldown"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                TransformDuration = new BetterOptionFloatItem().Create(RoleId + 20, SettingsTab, Translator.GetString("Role.Morphling.Option.TransformDuration"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem),
                AllowVenting = new BetterOptionCheckboxItem().Create(RoleId + 25, SettingsTab, Translator.GetString("Role.Ability.CanVent"), false, RoleOptionItem)
            ];
        }
    }

    public NetworkedPlayerInfo.PlayerOutfit? OriginalData { get; set; }
    public NetworkedPlayerInfo.PlayerOutfit? SampleData { get; set; }
    public TargetButton? SampleButton;
    public AbilityButton? TransformButton;
    public override void SetUpRole()
    {
        base.SetUpRole();
        OptionItems.Initialize();

        KillButton.TargetCondition = (PlayerControl target) =>
        {
            return !target.IsImpostorTeammate();
        };

        SampleButton = AddButton(new TargetButton().Create(5, Translator.GetString("Role.Morphling.Ability.1"), SampleCooldown.GetFloat(), 0, null, this, true, 1.2f)) as TargetButton;
        SampleButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.SampleData == null; };

        TransformButton = AddButton(new AbilityButton().Create(6, Translator.GetString("Role.Morphling.Ability.2"), TransformCooldown.GetFloat(), TransformDuration.GetFloat(), 0, null, this, true)) as AbilityButton;
        TransformButton.VisibleCondition = () => { return SampleButton.Role is MorphlingRole role && role.SampleData != null; };
        TransformButton.DurationName = Translator.GetString("Role.Morphling.Ability.3");
        TransformButton.CanCancelDuration = true;
    }
    public override void OnAbilityUse(int id, PlayerControl? target, Vent? vent)
    {
        switch (id)
        {
            case 5:
                if (target?.Data != null)
                {
                    SampleData = CopyOutfit(target.Data);
                    TransformButton.SetCooldown();
                }
                break;
            case 6:
                if (SampleData != null)
                {
                    OriginalData = CopyOutfit(_data);
                    SetOutfit(SampleData);
                    TransformButton.SetDuration();
                }
                break;
        }

        base.OnAbilityUse(id, target, vent);
    }

    public override void OnAbilityDurationEnd(int id)
    {
        switch (id)
        {
            case 6:
                if (OriginalData != null)
                {
                    SetOutfit(OriginalData);
                    SampleData = null;
                    OriginalData = null;
                    SampleButton.SetCooldown();
                }
                break;
        }
    }

    public NetworkedPlayerInfo.PlayerOutfit CopyOutfit(NetworkedPlayerInfo data)
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

    public void SetOutfit(NetworkedPlayerInfo.PlayerOutfit outfit)
    {
        _player.SetOutfit(outfit, PlayerOutfitType.Default);
    }
}


using Hazel;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class MinerRole : CustomRoleBehavior
{
    // Role Info
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Miner;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? DigCooldown;
    public BetterOptionItem? DigAmount;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DigCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Miner.Option.DigCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                DigAmount = new BetterOptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Miner.Option.DigAmount"), [0, 100, 1], 0, "", "", RoleOptionItem),
            ];
        }
    }

    private bool IsVisible { get; set; } = true;
    public AbilityButton? DigButton = new();
    public override void OnSetUpRole()
    {
        DigButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Miner.Ability.1"), DigCooldown.GetFloat(), 0, DigAmount.GetInt(), null, this, true));
        DigButton.InteractCondition = () => VentButton.ClosestObjDistance > 1f && !VentButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
        && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                Vector2 pos = reader != null ? NetHelpers.ReadVector2(reader) : _player.GetTruePosition();
                SpawnVent(pos);
                break;
        }
    }

    public override void AbilityWriter(int id, CustomRoleBehavior role, ref MessageWriter writer)
    {
        switch (id)
        {
            case 5:
                NetHelpers.WriteVector2(_player.GetTruePosition(), writer);
                break;
        }
    }

    private List<Vent> Vents = [];
    private void SpawnVent(Vector2 Pos)
    {
        var ventPrefab = ShipStatus.Instance.AllVents.First();
        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = "Vent(Miner)";

        vent.Id = GetAvailableId();
        var pos = Pos;
        float z = _player.gameObject.transform.position.z + 0.0005f;
        vent.transform.position = new Vector3(pos.x, pos.y, z);

        if (Vents.Count > 0)
        {
            var leftVent = Vents[^1];
            vent.Left = leftVent;
            leftVent.Right = vent;
        }
        else
        {
            vent.Left = null;
        }

        if (Vents.Count > 1)
        {
            Vents.First().Left = vent;
            vent.Right = Vents.First();
        }
        else
        {
            vent.Right = null;
        }
        vent.Center = null;

        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(vent);
        ShipStatus.Instance.AllVents = allVents.ToArray();

        // Play animation
        if (vent.myAnim != null)
        {
            vent.myAnim.Play(vent.ExitVentAnim, 1);
        }

        Vents.Add(vent);
    }

    private int GetAvailableId()
    {
        var id = 0;

        while (true)
        {
            if (ShipStatus.Instance.AllVents.All(v => v.Id != id)) return id;
            id++;
        }
    }
}

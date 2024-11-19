
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class MinerRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 4;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Miner;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override bool VentReliantRole => true;
    public override TBROptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public TBROptionItem? DigCooldown;
    public TBROptionItem? DigAmount;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DigCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Miner.Option.DigCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                DigAmount = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Miner.Option.DigAmount"), [0, 100, 1], 0, "", "", RoleOptionItem),
            ];
        }
    }

    private bool IsVisible { get; set; } = true;
    public BaseAbilityButton? DigButton = new();
    public override void OnSetUpRole()
    {
        DigButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Miner.Ability.1"), DigCooldown.GetFloat(), 0, DigAmount.GetInt(), null, this, true));
        DigButton.InteractCondition = () => VentButton.ClosestObjDistance > 1f && !VentButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
        && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);
        tempVentId = GetNoneVentId();
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                if (_player.IsLocalPlayer())
                {
                    tempVentId++;
                    if (tempVentId >= 50) tempVentId = 0;
                    var nextId = GetRoleVentId() + tempVentId;
                    Vector2 pos = _player.GetTruePosition();

                    SpawnVent(pos, nextId);
                    SendRoleSync(0, [pos, nextId]);
                }
                break;
        }
    }

    private List<Vent> Vents = [];
    private void SpawnVent(Vector2 Pos, int ventId)
    {
        var ventPrefab = ShipStatus.Instance.AllVents.First();
        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = "Vent(Miner)";

        vent.Id = ventId;
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
        vent.myAnim?.Play(vent.ExitVentAnim, 1);

        Vents.Add(vent);
    }

    private int tempVentId;
    private int GetRoleVentId() => 100 * (_player.PlayerId + 1);
    private int GetNoneVentId()
    {
        var existingIds = Main.AllVents.Select(v => v.Id).ToHashSet();
        int count = 0;
        while (existingIds.Contains(GetRoleVentId() + count + 1))
        {
            count++;
        }

        return count;
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    NetHelpers.WriteVector2((Vector2)additionalParams[0], writer);
                    writer.WritePacked((int)additionalParams[1]);
                }
                break;
        }
    }

    public override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    var pos = NetHelpers.ReadVector2(reader);
                    var ventId = reader.ReadPackedInt32();
                    SpawnVent(pos, ventId);
                }
                break;
        }
    }
}

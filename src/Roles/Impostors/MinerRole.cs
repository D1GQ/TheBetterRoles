using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class MinerRole : ImpostorRoleTBR, IRoleAbilityAction
{
    internal sealed override int RoleId => 4;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Miner;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override bool VentReliantRole => true;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;

    internal OptionItem? DigCooldown;
    internal OptionItem? DigAmount;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DigCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Miner.Option.DigCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
                DigAmount = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Miner.Option.DigAmount", (0, 15, 1), 0, ("", ""), RoleOptions.RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private bool IsVisible { get; set; } = true;
    private readonly List<Vent> Vents = [];
    internal BaseAbilityButton? DigButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            DigButton = RoleButtons.AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Miner.Ability.1"), DigCooldown.GetFloat(), 0, DigAmount.GetInt(), null, this, true));
            DigButton.InteractCondition = () => RoleButtons.VentButton.ClosestObjDistance > 1f && !RoleButtons.VentButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
            && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                if (_player.IsLocalPlayer())
                {
                    Vector2 pos = _player.GetTruePosition();
                    SpawnVent(pos);
                }
                break;
        }
    }

    private void SpawnVent(Vector2 position, int? syncVentId = null, bool isSync = false)
    {
        var vent = ShipStatus.Instance.AllVents.First().Copy("Vent(Miner)", syncVentId);
        vent.transform.position = new Vector3(position.x, position.y, _player.gameObject.transform.position.z + 0.0005f);

        if (Vents.Count > 0)
        {
            var lastVent = Vents[^1];
            lastVent.Right = vent;
            vent.Left = lastVent;

            if (Vents.Count > 1)
            {
                var firstVent = Vents.First();
                firstVent.Left = vent;
                vent.Right = firstVent;
            }
            else
            {
                vent.Right = null;
            }
        }
        else
        {
            vent.Left = null;
            vent.Right = null;
        }

        vent.Center = null;

        vent.myAnim?.Play(vent.ExitVentAnim, 1);

        Vents.Add(vent);

        if (!isSync)
        {
            SendRoleSync(position, vent.Id);
        }
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        var pos = reader.ReadFast<Vector2>();
        var ventId = reader.ReadFast<int>();
        SpawnVent(pos, ventId, true);
    }
}

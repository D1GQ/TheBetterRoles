
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class MoleRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#862500";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Mole;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => true;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private bool IsVisible { get; set; } = true;
    public AbilityButton? DigButton = new();
    public override void OnSetUpRole()
    {
        DigButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Mole.Ability.1"), 0, 0, 0, null, this, true)) as AbilityButton;
        DigButton.InteractCondition = () => VentButton.closestDistance > 1f && !VentButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent();
    }

    public override void OnAbility(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                SpawnVent();
                break;
        }
    }

    private List<Vent> Vents = [];
    public void SpawnVent()
    {
        var ventPrefab = Main.AllVents.First();
        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = "Vent(Mole)";

        // Set usable for local player
        vent.enabled = _player.IsLocalPlayer();
        if (vent.transform.Find("Sprite") == null)
        {
            vent.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f);
            vent.GetComponent<SpriteRenderer>().color.SetAlpha(_player.IsLocalPlayer() ? 1 : 0);
        }
        else
        {
            vent.transform.Find("Sprite").GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f);
            vent.transform.Find("Sprite").GetComponent<SpriteRenderer>().color.SetAlpha(_player.IsLocalPlayer() ? 1 : 0);
        }

        vent.Id = GetAvailableId();
        var pos = _player.GetTruePosition();
        float z = _player.gameObject.transform.position.z + 1.5f;
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
        if (_player.IsLocalPlayer())
        {
            vent.myAnim.Play(vent.ExitVentAnim, 1);
        }

        Vents.Add(vent);
    }

    public int GetAvailableId()
    {
        var id = 0;

        while (true)
        {
            if (ShipStatus.Instance.AllVents.All(v => v.Id != id)) return id;
            id++;
        }
    }
}

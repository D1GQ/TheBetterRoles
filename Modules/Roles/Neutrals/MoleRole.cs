
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace TheBetterRoles;

public class MoleRole : CustomRoleBehavior
{
    // Role Info
    public override bool CanBeAssigned => false;
    public override string RoleColor => "#862500";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Mole;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override bool CanVent => true;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? MaximumVents;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                MaximumVents = new BetterOptionIntItem().Create(RoleId + 10, SettingsTab, "Max Vents", [2, 5, 1], 3, "", "", RoleOptionItem),
            ];
        }
    }

    private bool IsVisible { get; set; } = true;
    public AbilityButton? DigButton = new();
    public override void OnSetUpRole()
    {
        DigButton = AddButton(new AbilityButton().Create(6, Translator.GetString("Role.Mole.Ability.1"), 0, 0, MaximumVents.GetInt() + 1, null, this, true)) as AbilityButton;
        DigButton.InteractCondition = () => VentButton.closestDistance > 1f && !VentButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent();
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 6:
                Vector2 pos = reader != null ? NetHelpers.ReadVector2(reader) : _player.GetTruePosition();
                SpawnVent(pos);
                break;
        }
    }

    public override void AbilityWriter(int id, CustomRoleBehavior role, ref MessageWriter writer)
    {
        switch (id)
        {
            case 6:
                NetHelpers.WriteVector2(_player.GetTruePosition(), writer);
                break;
        }
    }

    private List<Vent> Vents = [];
    private void SpawnVent(Vector2 Pos)
    {
        if (Vents.Count >= MaximumVents.GetInt())
        {
            RemoveVent(Vents.First());
        }

        var ventPrefab = ShipStatus.Instance.AllVents.First();
        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = "Vent(Mole)";

        // Set usable for local player
        vent.enabled = _player.IsLocalPlayer();
        float Alpha = _player.IsLocalPlayer() ? 1 : 0;
        if (vent.transform.Find("Sprite") == null)
        {
            vent.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, Alpha);
        }
        else if (vent.transform.Find("Sprite") != null)
        {
            vent.transform.Find("Sprite").GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, Alpha);
        }

        vent.Id = GetAvailableId();
        var pos = Pos;
        float z = _player.gameObject.transform.position.z + 0.00005f;
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
        if (vent.myAnim != null && _player.IsLocalPlayer())
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

    private void RemoveVent(Vent vent)
    {
        vent.enabled = false;
        Vents.Remove(vent);
        _player.StartCoroutine(FadeVentOut(vent));
    }

    private IEnumerator FadeVentOut(Vent vent)
    {
        float fadeDuration = 1f;
        float fadeSpeed = 1f / fadeDuration;
        bool fading = true;

        if (_player.IsLocalPlayer())
        {
            while (fading)
            {
                fading = false;

                foreach (var renderer in vent.GetComponentsInChildren<SpriteRenderer>())
                {
                    Color currentColor = renderer.color;

                    float newAlpha = Mathf.Max(currentColor.a - fadeSpeed * Time.deltaTime, 0f);
                    renderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

                    if (newAlpha > 0f)
                    {
                        fading = true;
                    }
                }

                yield return null;
            }

            DigButton.AddUse();
        }

        UnityEngine.Object.Destroy(vent.gameObject);
    }
}

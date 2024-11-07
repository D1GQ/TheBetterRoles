using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class MoleRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 21;
    public override string RoleColor => "#862500";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Mole;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override bool CanVent => false;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? MaximumVents;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                MaximumVents = new BetterOptionIntItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Mole.Option.MaxVents"), [2, 5, 1], 1, "", "", RoleOptionItem),
            ];
        }
    }

    public BaseAbilityButton? DigButton = new();
    public VentAbilityButton? BurrowButton = new();
    public override void OnSetUpRole()
    {
        BurrowButton = AddButton(new VentAbilityButton().Create(5, Translator.GetString("Role.Mole.Ability.1"), 0, 0, 0, this, null, true, true));
        BurrowButton.VentCondition = (Vent vent) =>
        {
            return CanVentOptionItem.GetBool() || Vents.Select(vents => vents.Id).Contains(vent.Id);
        };

        DigButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Mole.Ability.2"), 0, 0, MaximumVents.GetInt() + 1, null, this, true));
        DigButton.InteractCondition = () => BurrowButton.ClosestObjDistance > 1f && !BurrowButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
        && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (_player.IsLocalPlayer())
                    {
                        if (!_player.inVent)
                        {
                            _player.SendRpcVent(vent.Id, false);
                        }
                        else
                        {
                            _player.SendRpcVent(vent.Id, true);
                        }
                    }
                }
                break;
            case 6:
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

    private List<Vent> Vents = [];

    public override void OnDeinitialize()
    {
        List<Vent> ventsToRemove = [];
        foreach (Vent vent in Vents)
        {
            ventsToRemove.Add(vent);
        }
        foreach (Vent vent in ventsToRemove)
        {
            RemoveVent(vent, false);
        }
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        List<Vent> ventsToRemove = [];
        foreach (Vent vent in Vents)
        {
            ventsToRemove.Add(vent);
        }
        foreach (Vent vent in ventsToRemove)
        {
            RemoveVent(vent, false);
            DigButton.AddUse();
        }
    }

    private void SpawnVent(Vector2 Pos, int ventId)
    {
        if (Vents.Count >= MaximumVents.GetInt())
        {
            RemoveVent(Vents.First());
        }

        var ventPrefab = ShipStatus.Instance.AllVents.First();
        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = "Vent(Mole)";

        // Set usable for local player
        vent.SetEnabled(_player.IsLocalPlayer());
        float Alpha = _player.IsLocalPlayer() ? 1 : 0;

        vent.EnterVentAnim = null;
        vent.ExitVentAnim = null;
        if (vent.myAnim != null) vent.myAnim.DestroyMono();
        var animator = vent.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            UnityEngine.Object.Destroy(animator);
        }

        var spriteObj = vent.transform.Find("Sprite");
        if (spriteObj != null)
        {
            spriteObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            spriteObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }

        vent.myRend.color = new Color(1f, 0.4f, 0.4f, Alpha);
        _ = new LateTask(() =>
        {
            vent.myRend.sprite = LoadAbilitySprite("Mole_Vent");
        }, 0.00005f, shoudLog: false);

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

        Vents.Add(vent);
    }

    private int tempVentId;
    private int GetRoleVentId() => 100 * (_player.PlayerId + 1);

    // Fix meeting breaking
    private void RemoveVent(Vent vent, bool shrink = true)
    {
        vent.SetEnabled(false);
        Vents.Remove(vent);

        if (shrink)
        {
            _player.BetterData().StartCoroutine(ShrinkVentOut(vent));
        }
        else
        {
            var allVents = ShipStatus.Instance.AllVents.ToList();
            allVents.RemoveAll(v => v.Id == vent.Id);
            ShipStatus.Instance.AllVents = allVents.ToArray();
            vent.DestroyObj();
        }
    }

    private IEnumerator ShrinkVentOut(Vent vent)
    {
        float shrinkDuration = 1f;
        Vector3 startScale = vent.transform.localScale;
        Vector3 targetScale = Vector3.zero;
        Vector3 startPosition = vent.transform.position;
        Vector3 targetPosition = startPosition - new Vector3(0, 0.15f, 0);

        if (_player.IsLocalPlayer())
        {
            float elapsedTime = 0f;

            while (elapsedTime < shrinkDuration)
            {
                // Increase elapsed time
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / shrinkDuration); // Interpolation factor from 0 to 1

                // Interpolate scale and position based on the interpolation factor
                vent.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                vent.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                yield return null;
            }

            DigButton.AddUse();
        }

        // Remove the vent from the list and destroy the game object
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.RemoveAll(v => v.Id == vent.Id);
        ShipStatus.Instance.AllVents = allVents.ToArray();
        vent.DestroyObj();
    }

}

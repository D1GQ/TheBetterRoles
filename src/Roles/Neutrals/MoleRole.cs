using BepInEx.Unity.IL2CPP.Utils;
using Cpp2IL.Core.Extensions;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class MoleRole : RoleClass, IRoleAbilityAction<Vent>, IRoleMeetingAction
{
    internal sealed override int RoleId => 21;
    internal sealed override string RoleColorHex => "#862500";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Mole;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override bool CanKill => true;
    internal sealed override bool CanVent => false;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? MaximumVents;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                MaximumVents = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Mole.Option.MaxVents", (2, 5, 1), 2, ("", ""), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private readonly List<Vent> vents = [];
    internal BaseAbilityButton? DigButton = new();
    internal VentAbilityButton? BurrowButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            BurrowButton = RoleButtons.AddButton(VentAbilityButton.Create(5, Translator.GetString("Role.Mole.Ability.1"), 0, 0, 0, this, null, true, true));
            BurrowButton.VentCondition = (Vent vent) =>
            {
                return RoleOptions.CanVentOptionItem.GetBool() || vents.Select(vents => vents.Id).Contains(vent.Id);
            };
            RoleButtons.RemoveButton(RoleButtons.VentButton);
            RoleButtons.VentButton = BurrowButton;

            DigButton = RoleButtons.AddButton(BaseAbilityButton.Create(6, Translator.GetString("Role.Mole.Ability.2"), 0, 0, MaximumVents.GetInt() + 1, null, this, true));
            DigButton.InteractCondition = () => BurrowButton.ClosestObjDistance > 1f && !BurrowButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
            && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);
        }
    }

    void IRoleAbilityAction<Vent>.OnAbility(int id, Vent target)
    {
        switch (id)
        {
            case 5:
                {
                    if (!_player.inVent)
                    {
                        _player.SendRpcVent(target.Id, false);
                    }
                    else
                    {
                        _player.SendRpcVent(target.Id, true);
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 6:
                Vector2 pos = _player.GetTruePosition();
                SpawnVent(pos);
                break;
        }
    }

    internal sealed override void OnDeinitialize()
    {
        List<Vent> ventsToRemove = vents.Clone();
        foreach (Vent vent in ventsToRemove)
        {
            RemoveVent(vent);
        }
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        List<Vent> ventsToRemove = vents.Clone();
        foreach (Vent vent in ventsToRemove)
        {
            RemoveVent(vent, false);
            DigButton?.AddUse();
        }
    }

    private void SpawnVent(Vector2 position, int? syncVentId = null, bool isSync = false)
    {
        if (vents.Count >= MaximumVents.GetInt())
        {
            var firstVent = vents.First();
            firstVent.Right.Left = null;
            RemoveVent(firstVent);
        }

        var vent = ShipStatus.Instance.AllVents.First().Copy("Vent(Mole)", syncVentId);

        bool isLocalPlayer = _player.IsLocalPlayer();
        /// vent.SetEnabled(isLocalPlayer);
        vent.myRend.color = new Color(1f, 0.4f, 0.4f, isLocalPlayer ? 1f : 0f);

        vent.EnterVentAnim = null;
        vent.ExitVentAnim = null;
        vent.myAnim?.DestroyMono();

        var animator = vent.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            UnityEngine.Object.Destroy(animator);
        }

        var spriteObj = vent.transform.Find("Sprite");
        if (spriteObj != null)
        {
            spriteObj.transform.localPosition = Vector3.zero;
            spriteObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }

        _ = new LateTask(() =>
        {
            vent.myRend.sprite = LoadAbilitySprite("Mole_Vent");
            float zPosition = _player.gameObject.transform.position.z + 0.0005f;
            vent.transform.position = new Vector3(position.x, position.y, zPosition);
        }, 0.00005f, shouldLog: false);

        if (vents.Count > 0)
        {
            var lastVent = vents[^1];
            vent.Left = lastVent;
            lastVent.Right = vent;

            if (vents.Count > 1)
            {
                var firstVent = vents.First();
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

        vents.Add(vent);

        if (!isSync)
        {
            Networked.SendRoleSync(0, position, vent.Id);
        }
    }

    private void RemoveVent(Vent vent, bool shrink = true)
    {
        vent.Id = -1;
        vent.SetEnabled(false);
        vents.Remove(vent);

        if (shrink)
        {
            CoroutineManager.Instance.StartCoroutine(CoShrinkVentOut(vent));
        }
        else
        {
            vent.Remove();
        }
    }

    private IEnumerator CoShrinkVentOut(Vent vent)
    {
        float shrinkDuration = 1f;
        Vector3 startScale = vent.transform.localScale;
        Vector3 targetScale = Vector3.zero;
        Vector3 startPosition = vent.transform.position;
        Vector3 targetPosition = startPosition - new Vector3(0, 0.15f, 0);

        float elapsedTime = 0f;

        while (elapsedTime < shrinkDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / shrinkDuration);

            vent.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            vent.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        if (DigButton != null)
        {
            DigButton.AddUse();
        }

        vent.Remove();
    }

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                {
                    var pos = data.MessageReader.ReadVector2();
                    var ventId = data.MessageReader.ReadPackedInt32();
                    SpawnVent(pos, ventId, true);
                }
                break;
        }
    }
}

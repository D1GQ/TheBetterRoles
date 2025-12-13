using BepInEx.Unity.IL2CPP.Utils;
using Cpp2IL.Core.Extensions;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Modules.CustomSystems;
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

    private Vent ventPrefab;
    private readonly List<Vent> vents = [];
    internal BaseAbilityButton? DigButton = new();
    internal VentAbilityButton? BurrowButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            BurrowButton = RoleButtons.AddButton(VentAbilityButton.Create(5, Translator.GetString("Role.Mole.Ability.1"), 0, 0, 0, this, null, true, true));
            bool canVent = RoleOptions.CanVentOptionItem.GetBool();
            BurrowButton.VentCondition = (Vent vent) =>
            {
                return canVent || vents.Select(vents => vents.Id).Contains(vent.Id);
            };
            BurrowButton.GetVents = () =>
            {
                if (canVent)
                {
                    return [.. ShipStatus.Instance.AllVents, .. vents];
                }
                else
                {
                    return [.. vents];
                }
            };
            RoleButtons.RemoveButton(RoleButtons.VentButton);
            RoleButtons.VentButton = BurrowButton;

            DigButton = RoleButtons.AddButton(BaseAbilityButton.Create(6, Translator.GetString("Role.Mole.Ability.2"), 0, 0, MaximumVents.GetInt() + 1, null, this, true));
            DigButton.InteractCondition = () => BurrowButton.ClosestObjDistance > 1f && !BurrowButton.ActionButton.canInteract && _player.CanMove && !_player.IsInVent()
            && !PhysicsHelpers.AnythingBetween(_player.GetTruePosition(), _player.GetTruePosition() - new Vector2(0.25f, 0.25f), Constants.ShipAndAllObjectsMask, false);

            ventPrefab = UnityEngine.Object.Instantiate(ShipStatus.Instance.AllVents.First());
            ventPrefab.gameObject.name = "VentPrefab(Mole)";
            ventPrefab.gameObject.SetActive(false);
            ventPrefab.transform.SetParent(VentFactorySystem.Instance.CustomVentsObj.transform, false);

            ventPrefab.myRend.color = new Color(1f, 0.4f, 0.4f);

            ventPrefab.UnsetVents();
            ventPrefab.EnterVentAnim = null;
            ventPrefab.ExitVentAnim = null;
            ventPrefab.myAnim?.DestroyMono();

            var animator = ventPrefab.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                UnityEngine.Object.Destroy(animator);
            }

            var spriteObj = ventPrefab.transform.Find("Sprite");
            if (spriteObj != null)
            {
                spriteObj.transform.localPosition = Vector3.zero;
                spriteObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            }

            ventPrefab.myRend.sprite = LoadAbilitySprite("Mole_Vent");
        }
    }

    void IRoleAbilityAction<Vent>.OnAbility(int id, Vent target)
    {
        switch (id)
        {
            case 5:
                {
                    // Check if the target vent is one of the mole's vents
                    if (!vents.Contains(target))
                    {
                        // Use normal venting
                        if (!_player.inVent)
                        {
                            _player.SendRpcVent(target.Id, false);
                        }
                        else
                        {
                            _player.SendRpcVent(target.Id, true);
                        }
                    }
                    else
                    {
                        // Use mole venting
                        if (!_player.inVent)
                        {
                            EnterHole(target);
                        }
                        else
                        {
                            ExitHole(target);
                        }
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
                SpawnHole(pos);
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

    private void SpawnHole(Vector2 position, int? syncVentId = null, bool isSync = false)
    {
        if (vents.Count >= MaximumVents.GetInt())
        {
            var firstVent = vents.First();
            firstVent.Right.Left = null;
            RemoveVent(firstVent);
        }

        var vent = UnityEngine.Object.Instantiate(ventPrefab, VentFactorySystem.Instance.CustomVentsObj.transform, false);
        vent.gameObject.name = "Vent(Mole)";
        vent.Id = -1;
        vent.transform.position = new(position.x, position.y, Utils.GetPlayerZPosAtVector2(position) + 0.03f);

        // Set up vent connections
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

        // Set up button actions
        for (int i = 0; i < vent.Buttons.Length; i++)
        {
            var index = i;
            var button = vent.Buttons[i];
            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                var narbyVent = vent.NearbyVents[index];
                if (narbyVent != null)
                {
                    vent.SetArrows(false);
                    _player.NetTransform.RpcSnapTo(narbyVent.transform.position + narbyVent.Offset);
                    narbyVent.SetArrows(true);
                }
            }));
        }

        vents.Add(vent);

        vent.gameObject.SetActive(true);
    }

    private void RemoveVent(Vent vent, bool shrink = true)
    {
        vent.Id = -1;
        vent.SetEnabled(false);
        vents.Remove(vent);

        if (shrink)
        {
            CoroutineManager.Scene.StartCoroutine(CoShrinkHoleOut(vent));
        }
        else
        {
            vent.DestroyObj();
        }
    }

    private IEnumerator CoShrinkHoleOut(Vent vent)
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

        vent.DestroyObj();
    }

    private void EnterHole(Vent vent)
    {
        Networked.SendRoleSync(0, (Vector2)(vent.transform.position + vent.Offset), vent.NumFramesUntilPlayerDisappears);
        _player.MyPhysics.StopAllCoroutines();
        _player.MyPhysics.StartCoroutine(CoEnterHole(vent));
    }

    private IEnumerator CoEnterHole(Vent vent)
    {
        if (MeetingHud.Instance)
        {
            yield break;
        }
        _player.NetTransform.SetPaused(true);
        _player.MyPhysics.inputHandler.enabled = true;
        _player.walkingToVent = true;
        _player.moveable = false;
        yield return _player.MyPhysics.WalkPlayerTo(vent.transform.position + vent.Offset, 0.01f, 1f, false);
        _player.inVent = true;
        DebugAnalytics.Instance.Analytics.VentUsed(_player.Data);
        vent.EnterVent(_player);
        _player.cosmetics.AnimateSkinEnterVent();
        yield return _player.MyPhysics.Animations.CoPlayEnterVentAnimation(vent.NumFramesUntilPlayerDisappears);
        _player.cosmetics.AnimateSkinIdle();
        _player.MyPhysics.Animations.PlayIdleAnimation();
        _player.Visible = false;
        _player.walkingToVent = false;
        _player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)(an =>
        {
            an.ToggleRenderer(false);
        }));
        _player.MyPhysics.inputHandler.enabled = false;
        vent.SetArrows(true);
        yield break;
    }

    private IEnumerator CoEnterHoleNetworked(Vector2 pos, int numFramesUntilPlayerDisappears)
    {
        if (MeetingHud.Instance)
        {
            yield break;
        }
        _player.NetTransform.SetPaused(true);
        _player.walkingToVent = true;
        _player.moveable = false;
        yield return _player.MyPhysics.WalkPlayerTo(pos, 0.01f, 1f, false);
        _player.inVent = true;
        DebugAnalytics.Instance.Analytics.VentUsed(_player.Data);
        _player.cosmetics.AnimateSkinEnterVent();
        yield return _player.MyPhysics.Animations.CoPlayEnterVentAnimation(numFramesUntilPlayerDisappears);
        _player.cosmetics.AnimateSkinIdle();
        _player.MyPhysics.Animations.PlayIdleAnimation();
        _player.Visible = false;
        _player.walkingToVent = false;
        _player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)(an =>
        {
            an.ToggleRenderer(false);
        }));
        yield break;
    }

    private void ExitHole(Vent vent)
    {
        Networked.SendRoleSync(1, (Vector2)vent.transform.position);
        _player.MyPhysics.StopAllCoroutines();
        _player.MyPhysics.StartCoroutine(CoExitHole(vent));
    }

    private IEnumerator CoExitHole(Vent vent)
    {
        vent.SetArrows(false);
        Vector2 vector = vent.transform.position;
        vector -= _player.Collider.offset;
        _player.NetTransform.SnapTo(vector);
        _player.MyPhysics.inputHandler.enabled = true;
        yield return vent.ExitVent(_player);
        _player.Visible = true;
        _player.inVent = false;
        _player.cosmetics.AnimateSkinExitVent();
        yield return _player.MyPhysics.Animations.CoPlayExitVentAnimation();
        _player.cosmetics.AnimateSkinIdle();
        _player.MyPhysics.Animations.PlayIdleAnimation();
        _player.moveable = true;
        _player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)(an =>
        {
            an.ToggleRenderer(true);
        }));
        _player.MyPhysics.inputHandler.enabled = false;
        _player.NetTransform.SetPaused(false);
        yield break;
    }

    private IEnumerator CoExitHoleNetworked(Vector2 pos)
    {
        Vector2 vector = pos;
        vector -= _player.Collider.offset;
        _player.NetTransform.SnapTo(vector);
        _player.Visible = true;
        _player.inVent = false;
        _player.cosmetics.AnimateSkinExitVent();
        yield return _player.MyPhysics.Animations.CoPlayExitVentAnimation();
        _player.cosmetics.AnimateSkinIdle();
        _player.MyPhysics.Animations.PlayIdleAnimation();
        _player.moveable = true;
        _player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)(an =>
        {
            an.ToggleRenderer(true);
        }));
        _player.NetTransform.SetPaused(false);
        yield break;
    }

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                {
                    Vector2 pos = data.MessageReader.ReadFast<Vector2>();
                    int numFramesUntilPlayerDisappears = data.MessageReader.ReadFast<int>();
                    _player.MyPhysics.StopAllCoroutines();
                    _player.MyPhysics.StartCoroutine(CoEnterHoleNetworked(pos, numFramesUntilPlayerDisappears));
                }
                break;
            case 1:
                {
                    Vector2 pos = data.MessageReader.ReadFast<Vector2>();
                    _player.MyPhysics.StopAllCoroutines();
                    _player.MyPhysics.StartCoroutine(CoExitHoleNetworked(pos));
                }
                break;
        }
    }
}

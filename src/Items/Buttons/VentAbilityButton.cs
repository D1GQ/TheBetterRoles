using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

internal class VentAbilityButton : BaseButton
{
    internal Action? OnClick;
    private bool isAbility { get; set; }
    internal Vent? lastTargetVent { get; set; }
    internal float HighlightDistance { get; set; } = 3.5f;
    internal bool IsAbility { get; set; }

    [HideFromIl2Cpp]
    internal Func<Vent, bool> VentCondition { get; set; } = (target) => true;

    [HideFromIl2Cpp]
    internal void AddVentCondition(Func<Vent, bool> additionalCondition)
    {
        var originalCondition = VentCondition;
        VentCondition = (Vent vent) =>
        {
            return originalCondition(vent) && additionalCondition(vent);
        };
    }

    /// <summary>
    /// Create ability button for dead bodies.
    /// </summary>
    /// <param name="id">Ability button Id.</param>
    /// <param name="name">Ability button name.</param>
    /// <param name="cooldown">Base cooldown for ability button.</param>
    /// <param name="duration">Base duration for ability button.</param>
    /// <param name="abilityUses">Base Ability Uses for ability button.</param>
    /// <param name="sprite">Sprite for ability button.</param>
    /// <param name="role">Role for ability button.</param>
    /// <param name="Right">Determines if the ability button should be on the left or right of the screen.</param>
    /// <param name="isAbility">Determines if this is a normal vent button or used for a ability, this probably shouldn't be hard coded ¯\_(ツ)_/¯</param>
    /// <param name="index">Determines the index and position order of the ability button.</param>
    [HideFromIl2Cpp]
    internal VentAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, RoleClass role, Sprite? sprite, bool isAbility = false, bool Right = true, int index = -1)
    {
        if (role != null && role._player?.IsLocalPlayer() is false or null) return this;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomVent({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var AbilityButton = HudManager.Instance.gameObject.AddComponent<VentAbilityButton>();
        AbilityButton.SetUp(id, name, cooldown, duration, abilityUses, role, sprite, isAbility, buttonObj);

        return AbilityButton;
    }

    [HideFromIl2Cpp]
    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, RoleClass role, Sprite? sprite, bool isAbility, GameObject buttonObj)
    {
        this.isAbility = isAbility;
        Distance = 0.8f;
        Role = role;
        Id = id;
        Name = name;
        Cooldown = cooldown;
        Duration = duration;
        Uses = abilityUses;
        Visible = true;
        IsAbility = isAbility;

        if (buttonObj != null)
        {
            Button = buttonObj.GetComponent<PassiveButton>();
            ActionButton = buttonObj.GetComponent<ActionButton>();
            Text = ActionButton.buttonLabelText;

            if (sprite == null)
            {
                switch (Role.RoleTeam)
                {
                    case RoleClassTeam.Impostor:
                        ActionButton.graphic.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Vent-1.png", 100f);
                        break;
                    case RoleClassTeam.Crewmate:
                        ActionButton.graphic.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Vent-2.png", 100f);
                        break;
                    case RoleClassTeam.Neutral:
                    case RoleClassTeam.Apocalypse:
                        ActionButton.graphic.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Vent-3.png", 100f);
                        break;
                }
            }
            else
            {
                ActionButton.graphic.sprite = sprite;
            }
            ActionButton.graphic.SetCooldownNormalizedUvs();

            OnClick = Click;
            Button.OnClick = new();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (CanInteractOnPress())
                {
                    OnClick.Invoke();
                }
            }));
        }

        ActionButton.SetDisabled();
        ActionButton.transform.Find("CommsDown").GetComponent<SpriteRenderer>().sprite = new();
        ActionButton.OverrideText(name);
        ActionButton.buttonLabelText.fontSizeMin = 4f;
        ActionButton.buttonLabelText.enableWordWrapping = false;
        ActionButton.buttonLabelText.SetOutlineColor(Role != null && !isAbility ? Utils.GetCustomRoleColor(Role.RoleType) : Color.black);

        if (abilityUses <= 0)
        {
            ActionButton.SetInfiniteUses();
        }
        else
        {
            InfiniteUses = false;
            ActionButton.SetUsesRemaining(abilityUses);
        }

        ActionButton.usesRemainingSprite.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Counter.png", 100f);
        ActionButton.usesRemainingSprite.color = Role != null ? Utils.GetCustomRoleColor(Role.RoleType) : Color.gray;

        allButtons.Add(this);
        OnSetUp();
    }

    internal override void Click()
    {
        if (!isAbility)
        {
            if (lastTargetVent != null)
            {
                if (!_player.inVent)
                {
                    _player.SendRpcVent(lastTargetVent.Id, false);
                    if (Duration > 0f)
                        SetDuration();
                }
                else
                {
                    _player.SendRpcVent(lastTargetVent.Id, true);
                    ResetState();
                }
            }
        }
        else
        {
            if (!IsDuration)
            {
                Role.CheckAndUseAbility(Id, lastTargetVent, TargetType.Vent);
            }
            else if (IsDuration)
            {
                ResetState();
            }
        }
    }

    internal override bool BaseInteractable() =>
        !_player.inMovingPlat && !_player.IsOnLadder() &&
        InteractCondition() &&
        (!ActionButton.isCoolingDown || IsDuration && CanCancelDuration);

    internal override void ButtonUpdate()
    {
        Visible = (_player.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        Vent? targetVent = null;

        if (Visible && (!IsDuration || !IsAbility))
        {
            List<Vent> validVents = GetObjectsInAbilityRange(
                Main.AllEnabledVents
                    .Where(vent => VentCondition(vent) && _player.inVent)
                    .ToList(),
                HighlightDistance,
                _player.inVent,
                vent => vent.transform.position,
                false);

            targetVent = validVents.FirstOrDefault();
        }

        bool distanceFlag1 = ClosestObjDistance <= HighlightDistance;
        bool distanceFlag2 = ClosestObjDistance <= Distance;

        lastTargetVent?.SetOutline(Color.white, false, false);
        targetVent?.SetOutline(Colors.HexToColor(Utils.GetCustomRoleTeamColorHex(Role.RoleTeam)), distanceFlag1, distanceFlag2);

        if (Visible && ShowHighLight)
        {
            lastTargetVent = targetVent;
        }

        bool flag = Uses != 0 || InfiniteUses;

        if (distanceFlag2 && flag && !_player.walkingToVent && BaseInteractable())
        {
            ActionButton.SetEnabled();
        }
        else
        {
            ActionButton.SetDisabled();
        }

        ActionButton.gameObject.SetActive(Visible);
    }

    internal override void OnRemoveButton()
    {
        lastTargetVent?.SetOutline(Color.white, false, false);
    }

    /// <summary>
    /// Set settings from role
    /// </summary>
    /// <param name="role"></param>
    [HideFromIl2Cpp]
    internal void SetFromRole(RoleClass role)
    {
        Cooldown = role.RoleOptions.VentCooldownOptionItem?.GetFloat() ?? Cooldown;
        Duration = role.RoleOptions.VentDurationOptionItem?.GetFloat() ?? Duration;
    }
}

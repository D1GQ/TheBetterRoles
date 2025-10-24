using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

internal class PlayerAbilityButton : BaseButton
{
    internal Action? OnClick;
    internal bool CheckCanBeInteracted { get; set; } = true;
    internal PlayerControl? lastTarget { get; set; }

    [HideFromIl2Cpp]
    internal Func<PlayerControl, bool> TargetCondition { get; set; } = (target) => true;

    [HideFromIl2Cpp]
    internal void AddTargetCondition(Func<PlayerControl, bool> additionalCondition)
    {
        var originalCondition = TargetCondition;
        TargetCondition = (PlayerControl target) =>
        {
            return originalCondition(target) && additionalCondition(target);
        };
    }

    /// <summary>
    /// Create ability button for players.
    /// </summary>
    /// <param name="id">Ability button Id.</param>
    /// <param name="name">Ability button name.</param>
    /// <param name="cooldown">Base cooldown for ability button.</param>
    /// <param name="duration">Base duration for ability button.</param>
    /// <param name="abilityUses">Base Ability Uses for ability button.</param>
    /// <param name="sprite">Sprite for ability button.</param>
    /// <param name="role">Role for ability button.</param>
    /// <param name="Right">Determines if the ability button should be on the left or right of the screen.</param>
    /// <param name="range">Determines the range of the ability.</param>
    /// <param name="index">Determines the index and position order of the ability button.</param>
    [HideFromIl2Cpp]
    internal PlayerAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, bool Right = true, float range = 1f, int index = -1)
    {
        if (role != null && role._player?.IsLocalPlayer() is false or null) return this;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var AbilityButton = HudManager.Instance.gameObject.AddComponent<PlayerAbilityButton>();
        AbilityButton.SetUp(id, name, cooldown, duration, abilityUses, sprite, role, range, buttonObj);

        return AbilityButton;
    }

    [HideFromIl2Cpp]
    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, float range, GameObject buttonObj)
    {
        Role = role;
        Id = id;
        Distance = range * 0.5f + 1f;
        Name = name;
        Cooldown = cooldown;
        Duration = duration;
        Uses = abilityUses;
        Visible = true;

        if (buttonObj != null)
        {
            Button = buttonObj.GetComponent<PassiveButton>();
            ActionButton = buttonObj.GetComponent<ActionButton>();
            Text = ActionButton.buttonLabelText;

            if (sprite != null)
            {
                ActionButton.graphic.sprite = sprite;
            }
            else
            {
                ActionButton.graphic.sprite = HudManagerPatch.catchedUseSprite;
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
        ActionButton.buttonLabelText.SetOutlineColor(Role != null ? Utils.GetCustomRoleColor(Role.RoleType) : Color.black);

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
        if (!IsDuration)
        {
            Role.CheckAndUseAbility(Id, lastTarget, TargetType.Player);
        }
        else if (IsDuration)
        {
            ResetState();
        }
    }

    internal override void ButtonUpdate()
    {
        Visible = (_player.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        PlayerControl? target = null;

        if (Visible && !IsDuration)
        {
            var targets = GetObjectsInAbilityRange(
                Main.AllPlayerControls
                    .Where(target => target.IsAlive() && target != _player && TargetCondition(target) && target.ExtendedPC().InteractableTarget
                    && !target.ExtendedPC().InteractableTargetQueue && (!CheckCanBeInteracted || target.CanBeInteracted()))
                    .ToList(),
                Distance,
                false,
                target => target.GetTruePosition());

            target = targets.FirstOrDefault();
        }

        bool distanceFlag = ClosestObjDistance <= Distance;
        target = distanceFlag ? target : null;

        lastTarget?.SetOutline(false);
        if (target != null && Visible && ShowHighLight)
        {
            target.SetOutline(true, _player.GetRoleColor());
        }
        lastTarget = target;

        bool flag = Uses != 0 || InfiniteUses;

        if (distanceFlag && flag && BaseInteractable())
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
        lastTarget?.SetOutline(false);
    }
}

using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

internal class DeadBodyAbilityButton : BaseButton
{
    internal Action? OnClick;
    internal DeadBody? lastDeadBody { get; set; }

    [HideFromIl2Cpp]
    internal Func<DeadBody, bool> DeadBodyCondition { get; set; } = (body) => true;

    [HideFromIl2Cpp]
    internal void AddDeadBodyCondition(Func<DeadBody, bool> additionalCondition)
    {
        var originalCondition = DeadBodyCondition;
        DeadBodyCondition = (DeadBody body) =>
        {
            return originalCondition(body) && additionalCondition(body);
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
    /// <param name="range">Determines the range of the ability.</param>
    /// <param name="index">Determines the index and position order of the ability button.</param>
    [HideFromIl2Cpp]
    internal static DeadBodyAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, bool Right = true, float range = 1f, int index = -1)
    {
        if (role != null && role._player?.IsLocalPlayer() is false or null) return null;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var deadBodyAbilityButton = HudManager.Instance.gameObject.AddComponent<DeadBodyAbilityButton>();
        deadBodyAbilityButton.SetUp(id, name, cooldown, duration, abilityUses, sprite, role, range, buttonObj);
        return deadBodyAbilityButton;
    }

    [HideFromIl2Cpp]
    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, float range, GameObject buttonObj)
    {
        Role = role;
        Id = id;
        Distance = range * 0.5f + 1;
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
            Role.CheckAndUseAbility(Id, lastDeadBody, TargetType.Body);
        }
        else if (IsDuration)
        {
            ResetState();
        }
    }

    internal override void ButtonUpdate()
    {
        Visible = (_player.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        DeadBody? target = null;

        if (Visible && !IsDuration)
        {
            var bodies = GetObjectsInAbilityRange(
                Main.AllDeadBodys
                    .Where(b => DeadBodyCondition(b))
                    .ToList(),
                Distance,
                false,
                b => b.transform.position);

            target = bodies.FirstOrDefault();
        }

        bool distanceFlag = ClosestObjDistance <= Distance;
        target = distanceFlag ? target : null;

        lastDeadBody?.SetOutline(false, Color.clear);
        if (target != null && Visible && ShowHighLight)
        {
            target.SetOutline(true, _player.GetRoleColor());
        }
        lastDeadBody = target;

        bool flag = Uses != 0 || InfiniteUses;

        if (distanceFlag && flag && BaseInteractable() || IsDuration && CanCancelDuration)
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
        lastDeadBody?.SetOutline(false, Color.clear);
    }
}

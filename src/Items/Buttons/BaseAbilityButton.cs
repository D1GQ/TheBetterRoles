using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

internal class BaseAbilityButton : BaseButton
{
    internal Action? OnClick;

    /// <summary>
    /// Create basic ability button.
    /// </summary>
    /// <param name="id">Ability button Id.</param>
    /// <param name="name">Ability button name.</param>
    /// <param name="cooldown">Base cooldown for ability button.</param>
    /// <param name="duration">Base duration for ability button.</param>
    /// <param name="abilityUses">Base Ability Uses for ability button.</param>
    /// <param name="sprite">Sprite for ability button.</param>
    /// <param name="role">Role for ability button.</param>
    /// <param name="Right">Determines if the ability button should be on the left or right of the screen.</param>
    /// <param name="index">Determines the index and position order of the ability button.</param>
    [HideFromIl2Cpp]
    internal static BaseAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, bool Right = true, int index = -1)
    {
        if (role != null && role._player?.IsLocalPlayer() is false or null) return null;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name.ToUpper()})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var abilityButton = HudManager.Instance.gameObject.AddComponent<BaseAbilityButton>();
        abilityButton.SetUp(id, name, cooldown, duration, abilityUses, sprite, role, buttonObj);

        return abilityButton;
    }

    [HideFromIl2Cpp]
    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, RoleClass role, GameObject buttonObj)
    {
        Role = role;
        Id = id;
        Name = name;
        Cooldown = cooldown;
        Uses = abilityUses;
        Visible = true;
        Duration = duration;

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
            Role.CheckAndUseAbility(Id, null, TargetType.None);
        }
        else if (IsDuration)
        {
            ResetState();
        }
    }

    internal override void ButtonUpdate()
    {
        Visible = (_player.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        bool flag = Uses != 0 || InfiniteUses;

        if (flag && BaseInteractable())
        {
            ActionButton.SetEnabled();
        }
        else
        {
            ActionButton.SetDisabled();
        }

        ActionButton.gameObject.SetActive(Visible);
    }
}

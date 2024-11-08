using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class BaseAbilityButton : BaseButton
{
    public Action? OnClick;
    public BaseAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, int index = -1)
    {
        if (role?._player?.IsLocalPlayer() is false or null) return this;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({Name.ToUpper()})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var AbilityButton = buttonObj.AddComponent<BaseAbilityButton>();
        AbilityButton.SetUp(id, name, cooldown, duration, abilityUses, sprite, role, buttonObj);

        return AbilityButton;
    }

    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, GameObject buttonObj)
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
            buttonObj.SetActive(true);

            if (sprite != null)
            {
                ActionButton.graphic.sprite = sprite;
            }
            else
            {
                ActionButton.graphic.sprite = HudManager.Instance.UseButton.graphic.sprite;
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

        ActionButton.transform.Find("CommsDown").GetComponent<SpriteRenderer>().sprite = new();
        ActionButton.OverrideText(name);
        ActionButton.buttonLabelText.fontSizeMin = 4f;
        ActionButton.buttonLabelText.enableWordWrapping = false;
        ActionButton.buttonLabelText.SetOutlineColor(Utils.GetCustomRoleColor(Role.RoleType));

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
        ActionButton.usesRemainingSprite.color = Utils.GetCustomRoleColor(Role.RoleType);

        allButtons.Add(this);
    }

    public override void Click()
    {
        if (State == 0)
        {
            Role.CheckAndUseAbility(Id, 0, TargetType.None);
        }
        else if (State == 1)
        {
            ResetState();
        }
    }

    public override void ButtonUpdate()
    {
        Visible = (PlayerControl.LocalPlayer.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

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

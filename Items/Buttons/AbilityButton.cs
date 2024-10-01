
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class AbilityButton : BaseButton
{
    public string DurationName { get; set; } = "";
    public float Duration { get; set; } = 0f;
    public bool CanCancelDuration { get; set; } = false;
    public override bool CanInteractOnPress() => base.CanInteractOnPress() && !ActionButton.isCoolingDown || CanCancelDuration && State > 0;
    public AbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, int index = -1)
    {
        Role = role;
        Id = id;
        Name = name;
        Cooldown = cooldown;
        Uses = abilityUses;
        Visible = true;
        Duration = duration;

        if (!_player.IsLocalPlayer()) return this;

        var buttonObj = UnityEngine.Object.Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

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
            ActionButton.graphic.SetCooldownNormalizedUvs();

            Button.OnClick.RemoveAllListeners();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (CanInteractOnPress())
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
            }));
        }

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

        return this;
    }

    public void SetDuration(float amount = -1)
    {
        if (!_player.IsLocalPlayer()) return;

        if (amount <= -1)
        {
            TempCooldown = Duration;
        }
        else
        {
            TempCooldown = amount;
        }

        if (DurationName != "") ActionButton.OverrideText(DurationName);
        State = 1;
    }

    public void ResetState()
    {
        if (State == 1)
        {
            if (!_player.IsLocalPlayer()) return;

            State = 0;
            SetCooldown();
            Text.SetText(Name);
            _player.ResetAbilityStateSync(Id);
        }
    }

    public override void Update()
    {
        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        if (TempCooldown > 0 && TempCooldown <= 5)
        {
        }

        if (TempCooldown > 0)
        {
            if (BaseCooldown()) TempCooldown -= Time.deltaTime;

            if (State == 0)
            {
                ActionButton.SetCoolDown(TempCooldown, Cooldown);
            }
            else if (State == 1)
            {
                ActionButton.SetFillUp(TempCooldown, Duration);
            }
        }
        else if (State == 1)
        {
            ResetState();
        }
        else
        {
            ActionButton.SetCoolDown(-1, 0);
        }

        bool flag1 = Uses != 0 || InfiniteUses;

        if (flag1 && BaseInteractable())
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

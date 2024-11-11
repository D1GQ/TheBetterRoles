using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class PlayerAbilityButton : BaseButton
{
    public Action? OnClick;
    public PlayerControl? lastTarget { get; set; }
    public Func<PlayerControl, bool> TargetCondition { get; set; } = (target) => true;
    public PlayerAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, float range = 1f, int index = -1)
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

    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, float range, GameObject buttonObj)
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
    }

    public override void Click()
    {
        if (State == 0)
        {
            Role.CheckAndUseAbility(Id, lastTarget.PlayerId, TargetType.Player);
        }
        else if (State == 1)
        {
            ResetState();
        }
    }

    public override void ButtonUpdate()
    {
        Visible = (PlayerControl.LocalPlayer.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        PlayerControl? target = null;

        if (Visible)
        {
            var targets = GetObjectsInAbilityRange(
                Main.AllPlayerControls
                    .Where(target => target.IsAlive() && !target.IsLocalPlayer() && TargetCondition(target) && target.RoleChecks(role => role.InteractableTarget))
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
            target.SetOutline(true, PlayerControl.LocalPlayer.GetRoleColor());
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

    public override void OnRemoveButton()
    {
        lastTarget?.SetOutline(false);
    }
}

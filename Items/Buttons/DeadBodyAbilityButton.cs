using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class DeadBodyAbilityButton : BaseButton
{
    public Action? OnClick;
    public DeadBody? lastDeadBody { get; set; }
    public Func<DeadBody, bool> DeadBodyCondition { get; set; } = (body) => true;
    public DeadBodyAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, float range = 1f, int index = -1)
    {
        if (role?._player?.IsLocalPlayer() is false or null) return this;

        var buttonObj = Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomAbility({name})";

        if (index > -1)
        {
            buttonObj.transform.SetSiblingIndex(index);
        }

        var AbilityButton = HudManager.Instance.gameObject.AddComponent<DeadBodyAbilityButton>();
        AbilityButton.SetUp(id, name, cooldown, duration, abilityUses, sprite, role, range, buttonObj);
        return AbilityButton;
    }

    private void SetUp(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, float range, GameObject buttonObj)
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
            Role.CheckAndUseAbility(Id, lastDeadBody.ParentId, TargetType.Body);
        }
        else if (State == 1)
        {
            ResetState();
        }
    }

    public override void ButtonUpdate()
    {
        Visible = (PlayerControl.LocalPlayer.IsAlive() || UseAsDead) && VisibleCondition() && BaseShow();

        DeadBody? target = null;

        if (Visible)
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
        if (target != null && Visible)
        {
            target.SetOutline(true, PlayerControl.LocalPlayer.GetRoleColor());
        }
        lastDeadBody = target;

        bool flag = Uses != 0 || InfiniteUses;

        if (distanceFlag && flag && BaseInteractable() || State > 0 && CanCancelDuration)
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
        lastDeadBody?.SetOutline(false, Color.clear);
    }
}

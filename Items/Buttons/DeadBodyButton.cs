
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class DeadBodyButton : BaseButton
{
    public float DeadBodyRange { get; set; }
    public DeadBody? lastDeadBody { get; set; }
    public Func<DeadBody, bool> DeadBodyCondition { get; set; } = (DeadBody body) => true;
    public DeadBodyButton Create(int id, string name, float cooldown, float duration, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, float range = 1f, int index = -1)
    {
        Role = role;
        Id = id;
        DeadBodyRange = range * 0.5f + 1f;
        Name = name;
        Cooldown = cooldown;
        Duration = duration;
        Uses = abilityUses;
        Visible = true;

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
            else
            {
                ActionButton.graphic.sprite = HudManager.Instance.UseButton.graphic.sprite;
            }
            ActionButton.graphic.SetCooldownNormalizedUvs();

            Button.OnClick.RemoveAllListeners();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (CanInteractOnPress())
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

        return this;
    }

    public override void Update()
    {
        base.Update();

        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        bool flag1 = true;

        DeadBody? target = null;
        float closestDistance = float.MaxValue;

        if (Visible)
        {
            var Bodys = GetObjectsInAbilityRange(Main.AllDeadBodys.Where(b => DeadBodyCondition(b)).ToList(), false, b => b.transform.position);
            foreach (var deadBody in Bodys)
            {
                float distance = Vector2.Distance(PlayerControl.LocalPlayer.GetCustomPosition(), deadBody.transform.position);
                if (distance < closestDistance && distance <= DeadBodyRange)
                {
                    closestDistance = distance;
                    target = deadBody;
                }
            }
        }

        flag1 = target != null;

        if (lastDeadBody != null)
        {
            lastDeadBody.SetOutline(false, Color.clear);
        }

        if (target != null && Visible)
        {
            target.SetOutline(true, PlayerControl.LocalPlayer.GetRoleColor());
            lastDeadBody = target;
        }

        bool flag2 = Uses != 0 || InfiniteUses;

        if (flag1 && flag2 && BaseInteractable() || State > 0 && CanCancelDuration)
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

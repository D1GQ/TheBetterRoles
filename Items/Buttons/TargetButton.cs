
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class TargetButton : BaseButton
{
    public PlayerControl? lastTarget { get; set; }
    public Func<PlayerControl, bool> TargetCondition { get; set; } = (PlayerControl target) => true;
    public TargetButton Create(int id, string name, float cooldown, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, float range = 1f, int index = -1)
    {
        Role = role;
        Id = id;
        Distance = range * 0.5f + 1f;
        Name = name;
        Cooldown = cooldown;
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
                        Role.CheckAndUseAbility(Id, lastTarget.PlayerId, TargetType.Player);
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

        allButtons.Add(this);
        return this;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        PlayerControl? target = null;

        if (Visible)
        {
            var targets = GetObjectsInAbilityRange(
                Main.AllPlayerControls
                    .Where(target => target.IsAlive() && !target.IsLocalPlayer() && TargetCondition(target) && CustomRoleManager.RoleChecks(target, role => role.InteractableTarget))
                    .ToList(),
                Distance,
                false,
                target => target.GetTruePosition());

            target = targets.FirstOrDefault();
        }

        bool distanceFlag = ClosestObjDistance <= Distance;
        target = distanceFlag ? target : null;

        lastTarget?.SetOutline(false);
        if (target != null && Visible)
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

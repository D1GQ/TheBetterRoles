using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class VentAbilityButton : BaseButton
{
    public Action? OnClick;
    private bool isAbility { get; set; }
    public Vent? lastTargetVent { get; set; }
    public float HighlightDistance { get; set; } = 3.5f;
    public bool IsAbility { get; set; }
    public Func<Vent, bool> VentCondition { get; set; } = (target) => true;
    public VentAbilityButton Create(int id, string name, float cooldown, float duration, int abilityUses, CustomRoleBehavior role, Sprite? sprite, bool isAbility = false, bool Right = true, int index = -1)
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

        if (!_player.IsLocalPlayer()) return this;

        var buttonObj = UnityEngine.Object.Instantiate(HudManager.Instance.AbilityButton.gameObject, Right ? HudManagerPatch.ButtonsRight.transform : HudManagerPatch.ButtonsLeft.transform);
        buttonObj.name = $"CustomVent({name})";

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

            if (sprite == null)
            {
                switch (Role.RoleTeam)
                {
                    case CustomRoleTeam.Impostor:
                        ActionButton.graphic.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Vent-1.png", 100f);
                        break;
                    case CustomRoleTeam.Crewmate:
                        ActionButton.graphic.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Vent-2.png", 100f);
                        break;
                    case CustomRoleTeam.Neutral:
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
            Button.OnClick.RemoveAllListeners();
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
        ActionButton.buttonLabelText.SetOutlineColor(Utils.HexToColor32(!isAbility ? Utils.GetCustomRoleTeamColor(Role.RoleTeam) : Role.RoleColor));

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

    public override void Click()
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
            if (State == 0)
            {
                Role.CheckAndUseAbility(Id, lastTargetVent.Id, TargetType.Vent);
            }
            else if (State == 1)
            {
                ResetState();
            }
        }
    }

    public override bool BaseInteractable() =>
        !_player.inMovingPlat && !_player.IsOnLadder() &&
        InteractCondition() &&
        (!ActionButton.isCoolingDown || State > 0 && CanCancelDuration);

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        Vent? targetVent = null;

        if (Visible)
        {
            List<Vent> validVents = GetObjectsInAbilityRange(
                Main.AllEnabledVents
                    .Where(vent => VentCondition(vent))
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
        targetVent?.SetOutline(Utils.HexToColor32(Utils.GetCustomRoleTeamColor(Role.RoleTeam)), distanceFlag1, distanceFlag2);

        if (Visible)
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

    public override void OnRemoveButton()
    {
        lastTargetVent?.SetOutline(Color.white, false, false);
    }
}


using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class TargetButton : BaseButton
{
    public float TargetRange {  get; set; }
    public PlayerControl? lastTarget {  get; set; }
    public Func<PlayerControl, bool> TargetCondition { get; set; } = (PlayerControl target) => true;
    public override bool CanInteractOnPress() => base.CanInteractOnPress() && !ActionButton.isCoolingDown;
    public TargetButton Create(int id, string name, float cooldown, int abilityUses, Sprite? sprite, CustomRoleBehavior role, bool Right = true, float range = 1f, int index = -1)
    {
        Role = role;
        Id = id;
        TargetRange = range * 0.5f + 1f;
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

        return this;
    }

    public List<PlayerControl> GetPlayersInAbilityRangeSorted(List<PlayerControl> outputList, bool ignoreColliders)
    {
        if (!_player.CanMove || Uses <= 0 && !InfiniteUses || State > 0)
        {
            return [];
        }

        outputList.Clear();
        float abilityDistance = TargetRange;
        float closeDistanceThreshold = 0.15f;
        Vector2 myPos = _player.GetTruePosition();

        List<PlayerControl> allPlayers = Main.AllAlivePlayerControls.Where(pc => !pc.IsLocalPlayer() && TargetCondition(pc) && pc.BetterData().RoleInfo.Role.InteractableTarget).ToList();

        for (int i = 0; i < allPlayers.Count; i++)
        {
            PlayerControl PlayerInfo = allPlayers[i];
            PlayerControl targetPlayer = PlayerInfo;

            if (targetPlayer && targetPlayer.Collider.enabled)
            {
                Vector2 vectorToPlayer = targetPlayer.GetCustomPosition() - myPos;
                float magnitude = vectorToPlayer.magnitude;

                if (magnitude <= abilityDistance)
                {
                    if (magnitude <= closeDistanceThreshold || ignoreColliders ||
                        !PhysicsHelpers.AnyNonTriggersBetween(myPos, vectorToPlayer.normalized, magnitude, Constants.ShipAndObjectsMask))
                    {
                        outputList.Add(targetPlayer);
                    }
                }
            }
        }

        outputList.Sort((PlayerControl a, PlayerControl b) =>
        {
            float distA = (a.GetTruePosition() - myPos).magnitude;
            float distB = (b.GetTruePosition() - myPos).magnitude;
            return distA.CompareTo(distB);
        });

        return outputList;
    }


    public override void Update()
    {
        base.Update();

        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        bool flag1 = true;

        PlayerControl? target = null;
        float closestDistance = float.MaxValue;

        if (Visible)
        {
            foreach (var player in GetPlayersInAbilityRangeSorted([], false))
            {
                float distance = Vector2.Distance(PlayerControl.LocalPlayer.GetCustomPosition(), player.GetCustomPosition());
                if (distance < closestDistance && distance <= TargetRange)
                {
                    closestDistance = distance;
                    target = player;
                }
            }
        }

        flag1 = target != null;

        if (lastTarget != null)
        {
            lastTarget.SetOutline(false);
        }

        if (target != null && Visible)
        {
            target.SetOutline(true, PlayerControl.LocalPlayer.GetRoleColor());
            lastTarget = target;
        }

        bool flag2 = Uses != 0 || InfiniteUses;

        if (flag1 && flag2 && BaseInteractable())
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

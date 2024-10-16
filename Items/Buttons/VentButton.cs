using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class VentButton : BaseButton
{
    public Vent? lastTargetVent { get; set; }
    public float closestDistance = float.MaxValue;
    public float HighlightDistance { get; set; } = 3.5f;
    public float Distance { get; set; } = 0.8f;
    public bool IsAbility { get; set; }
    public Func<Vent, bool> VentCondition { get; set; } = (Vent target) => true;
    public override bool CanInteractOnPress() => base.CanInteractOnPress() && !ActionButton.isCoolingDown;
    public VentButton Create(int id, string name, float cooldown, int abilityUses, CustomRoleBehavior role, Sprite? sprite, bool isAbility = false, bool Right = true, int index = -1)
    {
        Role = role;
        Id = id;
        Name = name;
        Cooldown = cooldown;
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

            Button.OnClick.RemoveAllListeners();
            Button.OnClick.AddListener((Action)(() =>
            {
                if (CanInteractOnPress())
                {
                    if (!isAbility)
                    {
                        if (!_player.inVent)
                        {
                            _player.VentSync(lastTargetVent.Id, false);
                        }
                        else
                        {
                            _player.VentSync(lastTargetVent.Id, true);
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

        return this;
    }

    public override bool BaseInteractable() => !_player.inMovingPlat && !_player.IsOnLadder();

    public List<Vent> GetVentsInAbilityRangeSorted(List<Vent> outputList, bool ignoreColliders)
    {
        if (!_player.CanMove && !_player.IsInVent() || ShipStatus.Instance == null || Uses <= 0 && !InfiniteUses || State > 0)
        {
            return [];
        }

        outputList.Clear();
        float closeDistanceThreshold = 0.335f;
        Vector2 myPos = _player.GetTruePosition();

        List<Vent> allVents = Main.AllVents.Where(vent => VentCondition(vent)).ToList();

        for (int i = 0; i < allVents.Count; i++)
        {
            Vent vent = allVents[i];
            if (vent != null)
            {
                Vector2 vectorToVent = (Vector2)vent.transform.position - myPos;
                float magnitude = vectorToVent.magnitude;

                if (magnitude <= closeDistanceThreshold || ignoreColliders ||
                    !PhysicsHelpers.AnyNonTriggersBetween(myPos, vectorToVent.normalized, magnitude, Constants.ShipAndObjectsMask))
                {
                    outputList.Add(vent);
                }
            }
        }

        outputList.Sort((Vent a, Vent b) =>
        {
            float distA = ((Vector2)a.transform.position - myPos).magnitude;
            float distB = ((Vector2)b.transform.position - myPos).magnitude;
            return distA.CompareTo(distB);
        });

        return outputList;
    }


    public override void Update()
    {
        base.Update();

        Visible = UseAsDead == !PlayerControl.LocalPlayer.IsAlive() && VisibleCondition() && BaseShow();

        Vent? targetVent = null;
        closestDistance = float.MaxValue;

        if (Visible)
        {
            foreach (var vent in GetVentsInAbilityRangeSorted([], _player.inVent))
            {
                float distance = Vector2.Distance(PlayerControl.LocalPlayer.GetCustomPosition(), vent.transform.position);
                if (distance < closestDistance && distance <= HighlightDistance)
                {
                    closestDistance = distance;
                    targetVent = vent;
                }
            }
        }

        bool ventFlag1 = closestDistance <= HighlightDistance;
        bool ventFlag2 = closestDistance <= Distance;

        if (lastTargetVent != null)
        {
            lastTargetVent.SetOutline(Color.white, false, false);
        }
        targetVent.SetOutline(Utils.HexToColor32(Utils.GetCustomRoleTeamColor(Role.RoleTeam)), ventFlag1, ventFlag2);

        if (Visible)
        {
            lastTargetVent = targetVent;
        }

        bool flag = Uses != 0 || InfiniteUses;

        if (ventFlag2 && flag && !_player.walkingToVent && BaseInteractable())
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

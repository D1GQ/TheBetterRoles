using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Roles;
using TheBetterRoles.RPCs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

public class BaseButton : MonoBehaviour
{
    public static List<BaseButton> allButtons = [];

    // Player and Role information
    public int Id { get; protected set; }
    public CustomRoleBehavior? Role { get; protected set; }
    public PlayerControl? _player => Role != null ? Role._player : PlayerControl.LocalPlayer;
    public List<CustomAddonBehavior>? Addons => Role?._player?.ExtendedData()?.RoleInfo?.Addons;

    // Button and UI elements
    public ActionButton? ActionButton { get; protected set; }
    public PassiveButton? Button { get; protected set; }
    public TextMeshPro? Text { get; protected set; }
    public Func<bool> VisibleCondition = () => true;
    public Func<bool> InteractCondition = () => true;

    // Properties related to visibility and interaction
    public bool ShowHighLight { get; set; } = true;
    public bool UseAsDead { get; set; }
    public bool Visible { get; set; }
    public bool Hacked { get; set; }

    // Distances for interaction
    public float ClosestObjDistance { get; set; } = float.MaxValue;
    public float Distance { get; set; } = 0.8f;

    // Ability configuration
    public string? Name { get; protected set; } = "Ability";
    public IntRange Range = new(0, 100);
    public bool IsDuration { get; set; }
    public bool InfiniteUses { get; protected set; } = true;
    public int Uses { get; protected set; } = 0;

    // Cooldown and duration settings
    public float Cooldown { get; set; } = 25;
    public float TempTimer { get; protected set; } = 0f;
    public string DurationName { get; set; } = "";
    public float Duration { get; set; } = 0f;
    public bool CanCancelDuration { get; set; } = false;
    public bool HasDuration => Duration > 0f;

    public void AddVisibleCondition(Func<bool> additionalCondition)
    {
        var originalCondition = VisibleCondition;
        VisibleCondition = () =>
        {
            return originalCondition() && additionalCondition();
        };
    }

    // Core interaction logic
    public virtual bool BaseShow() =>
        !(GameState.IsMeeting || GameState.IsExilling) &&
        (MapBehaviour.Instance == null || !MapBehaviour.Instance.IsOpen);

    public virtual bool CanInteractOnPress() =>
        (ActionButton.canInteract && !ActionButton.isCoolingDown ||
        CanCancelDuration && IsDuration) && BaseInteractable() && !Hacked;

    public virtual bool BaseInteractable() =>
        !_player.IsInVent() && !_player.inMovingPlat && !_player.IsOnLadder() &&
        InteractCondition() &&
        (!ActionButton.isCoolingDown || IsDuration && CanCancelDuration);

    public virtual bool BaseCooldown() =>
        !(IsDuration && (_player.inMovingPlat || _player.IsOnLadder()) && TempTimer <= 3f) && GameManager.Instance.GameHasStarted;

    public List<T> GetObjectsInAbilityRange<T>(List<T> List, float maxDistance, bool ignoreColliders, Func<T, Vector3> posSelector, bool checkVent = true)
    {
        if (_player == null || !checkVent && !_player.CanMove && !_player.IsInVent() ||
            checkVent && _player.IsInVent() || Uses <= 0 && !InfiniteUses || IsDuration && !CanCancelDuration
            || !BaseInteractable())
        {
            return [];
        }

        List<T> allObjects = List;
        List<T> outputList = [];
        float closeDistanceThreshold = maxDistance;
        Vector2 myPos = _player.GetTruePosition();

        for (int i = 0; i < allObjects.Count; i++)
        {
            T obj = allObjects[i];
            if (obj != null)
            {
                Vector3 objPos3D = posSelector(obj);
                Vector2 objPos = new(objPos3D.x, objPos3D.y);
                Vector2 vectorToObj = objPos - myPos;
                float magnitude = vectorToObj.magnitude;

                if (magnitude <= closeDistanceThreshold && (ignoreColliders ||
                    !PhysicsHelpers.AnyNonTriggersBetween(myPos, vectorToObj.normalized, magnitude, Constants.ShipAndObjectsMask)))
                {
                    outputList.Add(obj);
                }
            }
        }

        ClosestObjDistance = float.MaxValue;

        if (outputList.Count == 1)
        {
            Vector3 pos3D = posSelector(outputList[0]);
            float dist = (new Vector2(pos3D.x, pos3D.y) - myPos).magnitude;
            ClosestObjDistance = dist;
        }
        else
        {
            outputList.Sort((a, b) =>
            {
                Vector3 posA3D = posSelector(a);
                Vector3 posB3D = posSelector(b);
                float distA = (new Vector2(posA3D.x, posA3D.y) - myPos).magnitude;
                float distB = (new Vector2(posB3D.x, posB3D.y) - myPos).magnitude;
                ClosestObjDistance = Mathf.Min(ClosestObjDistance, distA, distB);
                return distA.CompareTo(distB);
            });
        }

        return outputList;
    }

    public virtual void Click() { }

    public void FixedUpdate()
    {
        if (!Hacked)
        {
            if (TempTimer > 0)
            {
                if (BaseCooldown()) TempTimer -= Time.deltaTime;

                if (!IsDuration)
                {
                    ActionButton.SetCoolDown(TempTimer, Cooldown);
                }
                else if (IsDuration)
                {
                    ActionButton.SetFillUp(TempTimer, Duration);
                }
            }
            else if (IsDuration && (Duration > 0 || !CanCancelDuration))
            {
                ResetState(true);
            }
            else
            {
                ActionButton.SetCoolDown(-1, 0);
            }
        }

        ButtonUpdate();
    }
    public virtual void ButtonUpdate() { }

    public void RemoveButton()
    {
        allButtons.Remove(this);
        if (_player.IsLocalPlayer())
        {
            Button.DestroyObj();
        }

        OnRemoveButton();
        this.DestroyMono();
    }

    public virtual void OnRemoveButton() { }

    protected void ResetState(bool isTimeOut = false)
    {
        if (IsDuration)
        {
            if (!_player.IsLocalPlayer()) return;

            IsDuration = false;
            SetCooldown();
            Text.SetText(Name);
            if (Role != null) RPC.SendRpcResetAbilityState(_player, Id, isTimeOut, Role.RoleHash);
        }
    }

    public virtual void SetCooldown(float amount = -1, int durationState = -1)
    {
        if (durationState >= 0) IsDuration = durationState > 0;
        if (!IsDuration) ActionButton?.OverrideText(Name);
        if (IsDuration) return;

        if (amount <= -1)
        {
            TempTimer = Cooldown;
        }
        else
        {
            TempTimer = amount;
        }
    }

    public void SetDuration(float amount = -1)
    {
        if (!_player.IsLocalPlayer()) return;

        if (amount <= -1)
        {
            TempTimer = Duration;
        }
        else
        {
            TempTimer = amount;
        }

        if (DurationName != "") ActionButton.OverrideText(DurationName);
        IsDuration = true;
    }

    public void SetUses(int Amount)
    {
        if (Amount >= 0)
        {
            InfiniteUses = false;
            if (Uses + Amount <= Range.max)
            {
                Uses = Amount;
                ActionButton.SetUsesRemaining(Uses);
            }
        }
        else
        {
            InfiniteUses = true;
            ActionButton.SetInfiniteUses();
        }
    }

    public void AddUse(int Amount = 1)
    {
        if (!InfiniteUses)
        {
            if (Uses + Amount <= Range.max)
            {
                Uses += Amount;
                ActionButton.SetUsesRemaining(Uses);
            }
        }
    }

    public void RemoveUse(int Amount = 1)
    {
        if (!InfiniteUses)
        {
            if (Uses - Amount >= Range.min)
            {
                Uses -= Amount;
                ActionButton.SetUsesRemaining(Uses);
            }
        }
    }
}

using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class BaseButton
{
    public static List<BaseButton> allButtons = [];
    public CustomRoleBehavior? Role { get; set; }
    public List<CustomAddonBehavior>? Addons => Role._player.BetterData().RoleInfo.Addons;
    public PlayerControl? _player => Role._player;
    public ActionButton? ActionButton { get; set; }
    public PassiveButton? Button { get; set; }
    public TextMeshPro? Text { get; set; }
    public Func<bool> VisibleCondition = () => true;
    public int Id { get; set; }
    public bool UseAsDead { get; set; }
    public bool Visible { get; set; }
    public float ClosestObjDistance { get; set; } = float.MaxValue;
    public float Distance { get; set; } = 0.8f;

    public bool Hacked { get; set; }

    public string? Name { get; set; } = "Ability";
    public IntRange Range = new(0, 100);
    public int State = 0;

    public float Cooldown = 25;
    public float TempCooldown = 0f;
    public string DurationName = "";
    public float Duration = 0f;
    public bool CanCancelDuration = false;
    public bool InfiniteUses = true;
    public int Uses = 0;

    public bool HasDuration => Duration > 0f;
    public Func<bool> InteractCondition { get; set; } = () => true;
    public virtual bool BaseShow() =>
        !(GameStates.IsMeeting || GameStates.IsExilling) &&
        (MapBehaviour.Instance == null || MapBehaviour.Instance.IsOpen == false);

    public virtual bool CanInteractOnPress() => (ActionButton.canInteract && !ActionButton.isCoolingDown && BaseInteractable() || CanCancelDuration && State > 0) && !Hacked;
    public virtual bool BaseInteractable() => !_player.IsInVent() && !_player.inMovingPlat && !_player.IsOnLadder()
        && InteractCondition() && !ActionButton.isCoolingDown || CanCancelDuration && State > 0;
    public virtual bool BaseCooldown() => !_player.inMovingPlat && !_player.IsOnLadder() && GameManager.Instance.GameHasStarted;

    public List<T> GetObjectsInAbilityRange<T>(List<T> List, float maxDistance, bool ignoreColliders, Func<T, Vector3> posSelector, bool checkVent = true)
    {
        if ((!checkVent && !_player.CanMove && !_player.IsInVent()) ||
            (checkVent && _player.IsInVent()) || (Uses <= 0 && !InfiniteUses) || State > 0
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
            outputList.Sort((T a, T b) =>
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

    public virtual void FixedUpdate()
    {
        if (Hacked) return;

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
            ResetState(true);
        }
        else
        {
            ActionButton.SetCoolDown(-1, 0);
        }
    }

    public void RemoveButton()
    {
        allButtons.Remove(this);
        if (_player.IsLocalPlayer())
        {
            UnityEngine.Object.Destroy(Button.gameObject);
        }

        OnRemoveButton();
    }

    public virtual void OnRemoveButton() { }

    public virtual void SetCooldown(float amount = -1, int state = -1)
    {
        if (state >= 0) State = state;
        if (State > 0) return;

        if (amount <= -1)
        {
            TempCooldown = Cooldown;
        }
        else
        {
            TempCooldown = amount;
        }
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

    public void ResetState(bool isTimeOut = false)
    {
        if (State == 1)
        {
            if (!_player.IsLocalPlayer()) return;

            State = 0;
            SetCooldown();
            Text.SetText(Name);
            _player.ResetAbilityStateSync(Id, (int)Role.RoleType, isTimeOut);
        }
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

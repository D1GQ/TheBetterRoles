using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles.Core;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Items.Buttons;

/// <summary>
/// This is the base class for the ability buttons that will be used in the UI of the game.
/// The button interacts with the player's role and actions within the game, managing cooldowns, visibility,
/// and user interaction behavior.
/// </summary>
internal class BaseButton : MonoBehaviour
{
    internal static List<BaseButton> allButtons = [];

    // Player and Role information
    internal int Id { get; set; }
    [HideFromIl2Cpp]
    internal RoleClass? Role { get; set; }
    internal PlayerControl? overridePlayer;
    internal PlayerControl? _player => overridePlayer != null ? overridePlayer : Role != null ? Role._player : PlayerControl.LocalPlayer;
    [HideFromIl2Cpp]
    internal List<AddonClass>? Addons => Role?._player?.ExtendedData()?.RoleInfo?.Addons;

    // Button and UI elements
    internal ActionButton? ActionButton { get; set; }
    internal PassiveButton? Button { get; set; }
    internal TextMeshPro? Text { get; set; }
    internal Func<bool> VisibleCondition = () => true;
    internal Func<bool> InteractCondition = () => true;

    // Properties related to visibility and interaction
    internal bool ShowHighLight { get; set; } = true;
    internal bool UseAsDead { get; set; }
    internal bool Visible { get; set; }
    internal bool Hacked { get; set; }

    // Distances for interaction
    internal float ClosestObjDistance { get; set; } = float.MaxValue;
    internal float Distance { get; set; } = 0.8f;

    // Ability configuration
    internal string? Name { get; set; } = "Ability";
    internal IntRange Range = new(0, 100);
    internal bool IsDuration { get; set; }
    internal bool IsCooldown { get; set; }
    internal bool InfiniteUses { get; set; } = true;
    internal int Uses { get; set; } = 0;

    // Cooldown and duration settings
    internal float Cooldown { get; set; } = 25;
    internal float TempTimer { get; set; } = 0f;
    internal string DurationName { get; set; } = "";
    internal float Duration { get; set; } = 0f;
    internal bool CanCancelDuration { get; set; } = false;
    internal bool HasDuration => Duration > 0f;

    /// <summary>
    /// Add condition for the button to be visible.
    /// </summary>
    /// <param name="additionalCondition"></param>

    [HideFromIl2Cpp]
    internal void AddVisibleCondition(Func<bool> additionalCondition)
    {
        var originalCondition = VisibleCondition;
        VisibleCondition = () =>
        {
            return originalCondition() && additionalCondition();
        };
    }

    // Core interaction logic
    internal virtual bool BaseShow() =>
        !(GameState.IsMeeting || GameState.IsExilling) &&
        (MapBehaviour.Instance == null || !MapBehaviour.Instance.IsOpen);

    /// <summary>
    /// Determines if button will do interaction on press.
    /// </summary>
    /// <returns>bool</returns>
    internal virtual bool CanInteractOnPress() =>
        (ActionButton.canInteract && !ActionButton.isCoolingDown ||
        CanCancelDuration && IsDuration) && BaseInteractable() && !Hacked && Visible;

    /// <summary>
    /// Determines if button is interactable.
    /// </summary>
    /// <returns>bool</returns>
    internal virtual bool BaseInteractable() =>
        _player?.IsInVent() == false && _player?.inMovingPlat == false && _player?.IsOnLadder() == false &&
        InteractCondition() &&
        (!ActionButton.isCoolingDown || IsDuration && CanCancelDuration);

    /// <summary>
    /// Determines if the cooldown should be actively going down.
    /// </summary>
    /// <returns>bool</returns>
    internal virtual bool BaseCooldown() =>
        !(IsDuration && (_player?.inMovingPlat == true || _player?.IsOnLadder() == true) && TempTimer <= 3f) && GameManager.Instance?.GameHasStarted == true;

    /// <summary>
    /// Dynamically check if objects are in range of the player.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="List">List of objects</param>
    /// <param name="maxDistance">Minimum distance for object to be checked.</param>
    /// <param name="ignoreColliders">Determines if collision is checked.</param>
    /// <param name="posSelector">Function to extract position of object.</param>
    /// <param name="checkVent">Determines if objects should be looked for while the player is inside a vent.</param>
    /// <returns></returns>
    [HideFromIl2Cpp]
    internal List<T> GetObjectsInAbilityRange<T>(List<T> List, float maxDistance, bool ignoreColliders, Func<T, Vector3> posSelector, bool checkVent = true)
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

    internal void OnSetUp()
    {
        ActionButton.transform.position.Set(ActionButton.transform.position.x, ActionButton.transform.position.y, -5);
    }

    /// <summary>
    /// Implement logic when button is successfully clicked.
    /// </summary>
    internal virtual void Click() { }

    private void LateUpdate()
    {
        if (!Hacked)
        {
            if (TempTimer > 0)
            {
                if (BaseCooldown()) TempTimer -= Time.deltaTime;

                if (!IsDuration)
                {
                    ActionButton.SetCoolDown(TempTimer, Cooldown);
                    IsCooldown = true;
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
                IsCooldown = false;
            }
        }

        ButtonUpdate();
    }

    /// <summary>
    /// Implement logic that's constantly updated for the button.
    /// </summary>
    internal virtual void ButtonUpdate() { }

    /// <summary>
    /// Remove button from UI.
    /// </summary>
    internal void RemoveButton()
    {
        allButtons.Remove(this);
        if (_player.IsLocalPlayer())
        {
            Button?.DestroyObj();
        }

        OnRemoveButton();
        this.DestroyMono();
    }

    /// <summary>
    /// Implement logic when button is removed from UI.
    /// </summary>
    internal virtual void OnRemoveButton() { }

    /// <summary>
    /// Reset button state.
    /// </summary>
    /// <param name="isTimeOut">If state was force reset by duration timer.</param>
    protected void ResetState(bool isTimeOut = false)
    {
        if (IsDuration)
        {
            if (!_player.IsLocalPlayer()) return;

            IsDuration = false;
            Text?.SetText(Name);
            if (Role != null) RPC.SendRpcResetAbilityState(_player, Id, isTimeOut, Role.RoleHash);
        }

        SetCooldown();
    }

    /// <summary>
    /// Set cooldown time.
    /// </summary>
    /// <param name="amount">Set cooldown amount.</param>
    /// <param name="durationState">Set button state.</param>
    internal virtual void SetCooldown(float amount = -1, int durationState = -1)
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

    /// <summary>
    /// Set duration time.
    /// </summary>
    /// <param name="amount">Set duration amount.</param>
    internal void SetDuration(float amount = -1)
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

        if (DurationName != "") ActionButton?.OverrideText(DurationName);
        IsDuration = true;
    }

    /// <summary>
    /// Set amount of uses for ability. Anything below 0 is infinite.
    /// </summary>
    /// <param name="amount">Set amount of uses</param>
    internal void SetUses(int amount)
    {
        if (amount >= 0)
        {
            InfiniteUses = false;
            if (Uses + amount <= Range.max)
            {
                Uses = amount;
                ActionButton?.SetUsesRemaining(Uses);
            }
        }
        else
        {
            InfiniteUses = true;
            ActionButton?.SetInfiniteUses();
        }
    }

    private float gainedUses = 0f;
    /// <summary>
    /// Give use to ability button, number can be decimal.
    /// </summary>
    /// <param name="amount">Use gain</param>
    /// <param name="max">Maximum uses for ability</param>
    internal void GainUse(float amount, int max)
    {
        int currentUses = Uses;
        gainedUses += amount;
        if (gainedUses % 1 != 0)
        {
            return;
        }
        int maxAlerts = max;
        int newUses = Math.Clamp(currentUses + (int)gainedUses, 0, maxAlerts);
        SetUses(newUses);
        gainedUses = 0f;
    }

    /// <summary>
    /// Add use to ability button.
    /// </summary>
    /// <param name="amount">Amount of uses.</param>
    internal void AddUse(int amount = 1)
    {
        if (!InfiniteUses)
        {
            if (Uses + amount <= Range.max)
            {
                Uses += amount;
                ActionButton?.SetUsesRemaining(Uses);
            }
        }
    }

    /// <summary>
    /// Remove use to ability button.
    /// </summary>
    /// <param name="amount">Amount of uses.</param>
    internal void RemoveUse(int amount = 1)
    {
        if (!InfiniteUses)
        {
            if (Uses - amount >= Range.min)
            {
                Uses -= amount;
                ActionButton?.SetUsesRemaining(Uses);
            }
        }
    }
}

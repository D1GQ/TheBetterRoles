
using Hazel;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;
using static Sentry.MeasurementUnit;

namespace TheBetterRoles;

public class BaseButton
{
    public CustomRoleBehavior? Role { get; set; }
    public List<CustomAddonBehavior>? Addons => Role._player.BetterData().RoleInfo.Addons;
    public PlayerControl? _player => Role._player;
    public ActionButton? ActionButton { get; set; }
    public PassiveButton? Button { get; set; }
    public TextMeshPro? Text { get; set; }
    public Func<bool> VisibleCondition = () => true;
    public int Id { get; set; }
    public bool UseAsDead { get; set; }
    public bool Visible { get; set; } = false;

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

    public virtual bool CanInteractOnPress() => ActionButton.canInteract && !ActionButton.isCoolingDown || CanCancelDuration && State > 0;
    public virtual bool BaseInteractable() => !_player.IsInVent() && !_player.inMovingPlat && !_player.IsOnLadder() && InteractCondition() || CanCancelDuration && State > 0;
    public virtual bool BaseCooldown() => !_player.inMovingPlat && !_player.IsOnLadder() && GameManager.Instance.GameHasStarted;

    public virtual void Update() 
    {
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
        if (_player.IsLocalPlayer())
        {
            UnityEngine.Object.Destroy(Button.gameObject);
        }
    }

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

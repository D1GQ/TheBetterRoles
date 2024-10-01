
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

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
    public bool InfiniteUses = true;
    public int Uses = 0;

    public virtual bool BaseShow() =>
        !(GameStates.IsMeeting || GameStates.IsExilling) &&
        (MapBehaviour.Instance == null || MapBehaviour.Instance.IsOpen == false);

    public virtual bool CanInteractOnPress() => ActionButton.canInteract;
    public virtual bool BaseInteractable() => !_player.IsInVent() && !_player.inMovingPlat && !_player.IsOnLadder();
    public virtual bool BaseCooldown() => !_player.inMovingPlat && !_player.IsOnLadder() && GameManager.Instance.GameHasStarted;

    public virtual void Update() { }

    public void RemoveButton()
    {
        if (_player.IsLocalPlayer())
        {
            UnityEngine.Object.Destroy(Button.gameObject);
        }
    }

    public virtual void SetCooldown(float amount = -1, int state = 0)
    {
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

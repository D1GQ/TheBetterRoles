
using System;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;
using UnityEngine.UI;

namespace TheBetterRoles;

public abstract class CustomAddonBehavior : CustomRoleBehavior
{
    public virtual Func<CustomRoles, bool> AssignmentCondition => (CustomRoles role) => true;
    public override bool IsAddon => true;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => true;
    public override bool CanMoveInVents => true;
    public override void SetUpRole()
    {
        OptionItems.Initialize();
        OnSetUpRole();
    }
}

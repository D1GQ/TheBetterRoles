
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;
using UnityEngine.UI;

namespace TheBetterRoles;

public abstract class CustomAddonBehavior : CustomRoleBehavior
{
    public override bool IsAddon => true;
}
